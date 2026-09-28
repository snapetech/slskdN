// <copyright file="browser-process-resources.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as fs from 'node:fs/promises';
import * as path from 'node:path';

export type ProcessStat = {
  pid: number; parentPid: number; startTimeTicks: number; cpuTicks: number;
};
export type BrowserProcessSnapshot = {
  timestampSeconds: number;
  enumerationReadsUnavailable: number;
  processes: (ProcessStat & { pssMiB: number | null })[];
};

// /proc stat names can contain spaces and closing parentheses. Numeric fields
// follow the final closing parenthesis; start time distinguishes reused PIDs.
export function parseProcessStat(stat: string): ProcessStat {
  const pidText = stat.match(/^(\d+) \(/u)?.[1];
  const fields = stat.slice(stat.lastIndexOf(')') + 2).trim().split(/\s+/u);
  const values = [pidText, fields[1], fields[11], fields[12], fields[19]];
  if (values.some((value) => !value || !/^\d+$/u.test(value) || !Number.isSafeInteger(Number(value)))) {
    throw new Error('Invalid process stat');
  }
  const [pid, parentPid, userTicks, systemTicks, startTimeTicks] = values.map(Number);
  return { pid, parentPid, startTimeTicks, cpuTicks: userTicks + systemTicks };
}

export async function collectBrowserProcessSnapshot(rootPid: number, procDirectory = '/proc'): Promise<BrowserProcessSnapshot> {
  const timestampSeconds = performance.now() / 1000;
  const pids = (await fs.readdir(procDirectory)).filter((entry) => /^\d+$/u.test(entry));
  const stats: ProcessStat[] = [];
  let enumerationReadsUnavailable = 0;
  // Bound filesystem concurrency instead of opening a descriptor for every
  // process on the host. Failed reads remain visible in the coverage report.
  for (let index = 0; index < pids.length; index += 32) {
    const batch = await Promise.allSettled(pids.slice(index, index + 32).map(async (pid) =>
      parseProcessStat(await fs.readFile(path.join(procDirectory, pid, 'stat'), 'utf8'))));
    for (const result of batch) {
      if (result.status === 'fulfilled') stats.push(result.value);
      else enumerationReadsUnavailable += 1;
    }
  }
  if (!stats.some((stat) => stat.pid === rootPid)) throw new Error('Owned browser root is unavailable');
  const owned = new Set([rootPid]);
  let previousSize;
  do {
    previousSize = owned.size;
    for (const stat of stats) if (owned.has(stat.parentPid)) owned.add(stat.pid);
  } while (owned.size !== previousSize);
  const descendants = stats.filter((stat) => owned.has(stat.pid));
  const memory: PromiseSettledResult<number>[] = [];
  for (let index = 0; index < descendants.length; index += 32) {
    memory.push(...await Promise.allSettled(descendants.slice(index, index + 32).map(async (stat) => {
      const directory = path.join(procDirectory, String(stat.pid));
      const rollup = await fs.readFile(path.join(directory, 'smaps_rollup'), 'utf8');
      const pss = rollup.match(/^Pss:\s+(\d+) kB$/mu);
      if (!pss) throw new Error('Process PSS is unavailable');
      const current = parseProcessStat(await fs.readFile(path.join(directory, 'stat'), 'utf8'));
      if (current.pid !== stat.pid || current.startTimeTicks !== stat.startTimeTicks) {
        throw new Error('Process identity changed during memory sampling');
      }
      return Number(pss[1]) / 1024;
    })));
  }
  return { timestampSeconds, enumerationReadsUnavailable,
    processes: descendants.map((stat, index) => {
      const reading = memory[index];
      return { ...stat, pssMiB: reading.status === 'fulfilled' ? reading.value : null };
    }) };
}

export function compareBrowserProcessSnapshots(before: BrowserProcessSnapshot, after: BrowserProcessSnapshot, ticksPerSecond: number) {
  const seconds = after.timestampSeconds - before.timestampSeconds;
  if (!(seconds > 0) || !Number.isSafeInteger(ticksPerSecond) || ticksPerSecond <= 0) {
    throw new Error('Invalid process measurement interval or clock tick frequency');
  }
  const identity = (stat: ProcessStat) => `${stat.pid}:${stat.startTimeTicks}`;
  const previous = new Map(before.processes.map((stat) => [identity(stat), stat]));
  const current = new Set(after.processes.map(identity));
  const sampled = after.processes.filter((stat) => previous.has(identity(stat)));
  const ticks = sampled.reduce((sum, stat) => sum + Math.max(0, stat.cpuTicks - previous.get(identity(stat))!.cpuTicks), 0);
  const memory = after.processes.filter((stat) => stat.pssMiB !== null);
  return {
    seconds, cpuPercentOfOneCore: ticks / ticksPerSecond / seconds * 100,
    processesBefore: before.processes.length, processesAfter: after.processes.length,
    processesSampled: sampled.length,
    processesAdded: after.processes.length - sampled.length,
    processesRemoved: before.processes.filter((stat) => !current.has(identity(stat))).length,
    enumerationReadsUnavailableBefore: before.enumerationReadsUnavailable,
    enumerationReadsUnavailableAfter: after.enumerationReadsUnavailable,
    pssMiB: memory.length ? memory.reduce((sum, stat) => sum + stat.pssMiB!, 0) : null,
    memoryProcessesMeasured: memory.length,
    memoryProcessesUnavailable: after.processes.length - memory.length,
  };
}
