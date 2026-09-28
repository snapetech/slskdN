// <copyright file="browser-process-resources.test.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
// @vitest-environment node
import * as fs from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { collectBrowserProcessSnapshot, compareBrowserProcessSnapshots, parseProcessStat } from '../e2e/harness/browser-process-resources';

vi.mock('node:fs/promises', async (original) => {
  const real = await original<typeof import('node:fs/promises')>();
  return { ...real, readFile: vi.fn(real.readFile) };
});

const directories: string[] = [];
const stat = (pid: number, parent: number, start: number, user = 1, system = 2, name = 'chrome') => {
  const fields = Array(20).fill('0');
  fields[0] = 'S'; fields[1] = String(parent); fields[11] = String(user);
  fields[12] = String(system); fields[19] = String(start);
  return `${pid} (${name}) ${fields.join(' ')}`;
};
const fixture = async () => {
  const directory = await fs.mkdtemp(path.join(os.tmpdir(), 'browser-process-test-'));
  directories.push(directory);
  return directory;
};
const writeProcess = async (directory: string, pid: number, parent: number, pss: number | null) => {
  const target = path.join(directory, String(pid));
  await fs.mkdir(target);
  await fs.writeFile(path.join(target, 'stat'), stat(pid, parent, pid * 10));
  if (pss !== null) await fs.writeFile(path.join(target, 'smaps_rollup'), `Pss: ${pss} kB\n`);
};
afterEach(async () => {
  vi.mocked(fs.readFile).mockReset();
  const real = await vi.importActual<typeof import('node:fs/promises')>('node:fs/promises');
  vi.mocked(fs.readFile).mockImplementation(real.readFile);
  await Promise.all(directories.splice(0).map((directory) => fs.rm(directory, { recursive: true, force: true })));
});

describe('owned browser process resources', () => {
  it('parses names containing spaces and parentheses without shifting numeric fields', () => {
    expect(parseProcessStat(stat(12, 10, 999, 7, 8, 'chrome ) worker (zygote)')))
      .toEqual({ pid: 12, parentPid: 10, startTimeTicks: 999, cpuTicks: 15 });
  });

  it.each(['not a stat', '12 (chrome) S 10', stat(12, 10, 999).replace('999', 'invalid')])('rejects invalid external process data: %s', (input) => {
    expect(() => parseProcessStat(input)).toThrow('Invalid process stat');
  });

  it('includes descendants through zygotes and excludes unrelated processes', async () => {
    const directory = await fixture();
    await writeProcess(directory, 10, 0, 1024);
    await writeProcess(directory, 11, 10, 2048);
    await writeProcess(directory, 12, 11, 3072);
    await writeProcess(directory, 20, 0, 9999);
    const snapshot = await collectBrowserProcessSnapshot(10, directory);
    expect(snapshot.processes.map((entry) => entry.pid).sort()).toEqual([10, 11, 12]);
    expect(snapshot.processes.reduce((sum, entry) => sum + entry.pssMiB!, 0)).toBe(6);
    expect(snapshot.enumerationReadsUnavailable).toBe(0);
  });

  it('discloses missing stat and memory reads without reporting missing memory as zero', async () => {
    const directory = await fixture();
    await writeProcess(directory, 10, 0, null);
    await fs.mkdir(path.join(directory, '30'));
    const snapshot = await collectBrowserProcessSnapshot(10, directory);
    expect(snapshot.enumerationReadsUnavailable).toBe(1);
    expect(snapshot.processes[0].pssMiB).toBeNull();
    const result = compareBrowserProcessSnapshots({ ...snapshot, timestampSeconds: 1 }, { ...snapshot, timestampSeconds: 2 }, 100);
    expect(result.pssMiB).toBeNull();
    expect(result.memoryProcessesUnavailable).toBe(1);
  });

  it('fails when the owned root cannot be read', async () => {
    await expect(collectBrowserProcessSnapshot(10, await fixture())).rejects.toThrow('Owned browser root is unavailable');
  });

  it('does not charge memory from a PID reused during the snapshot', async () => {
    const directory = await fixture();
    await writeProcess(directory, 10, 0, 1024);
    const real = await vi.importActual<typeof import('node:fs/promises')>('node:fs/promises');
    vi.mocked(fs.readFile).mockImplementation(async (file, options) => {
      const result = await real.readFile(file, options);
      if (String(file).endsWith('smaps_rollup')) {
        await fs.writeFile(path.join(directory, '10', 'stat'), stat(10, 0, 999));
      }
      return result;
    });
    const snapshot = await collectBrowserProcessSnapshot(10, directory);
    expect(snapshot.processes[0].pssMiB).toBeNull();
  });

  it('excludes reused PIDs from CPU deltas and reports process churn', () => {
    const root = { pid: 10, parentPid: 0, startTimeTicks: 100, cpuTicks: 5, pssMiB: 1 };
    const child = { pid: 11, parentPid: 10, startTimeTicks: 101, cpuTicks: 10, pssMiB: 2 };
    const before = { timestampSeconds: 1, enumerationReadsUnavailable: 0, processes: [root, child] };
    const after = { timestampSeconds: 3, enumerationReadsUnavailable: 0, processes: [
      { ...root, cpuTicks: 15 }, { ...child, startTimeTicks: 222, cpuTicks: 99 },
      { ...child, pid: 12, startTimeTicks: 102, cpuTicks: 200 },
    ] };
    expect(compareBrowserProcessSnapshots(before, after, 100)).toMatchObject({
      cpuPercentOfOneCore: 5, processesSampled: 1, processesAdded: 2,
      processesRemoved: 1, memoryProcessesMeasured: 3, pssMiB: 5,
    });
  });

  it.each([0, -1, 0.5])('rejects an invalid tick frequency: %s', (ticks) => {
    const snapshot = { timestampSeconds: 1, enumerationReadsUnavailable: 0, processes: [] };
    expect(() => compareBrowserProcessSnapshots(snapshot, { ...snapshot, timestampSeconds: 2 }, ticks)).toThrow();
  });
});
