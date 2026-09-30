// <copyright file="LatencyTcpProxy.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as net from 'node:net';
import { Transform, type TransformCallback } from 'node:stream';
import { pipeline } from 'node:stream/promises';

type DelayDirection = 'listenerToHost' | 'hostToListener';
type DelayObservation = {
  chunks: number;
  minScheduledMs: number | null;
  maxScheduledMs: number | null;
  minObservedMs: number | null;
  maxObservedMs: number | null;
};

const jitterPattern = [0, -1, 1, -0.5, 0.5];

export class LatencyTcpProxy {
  private readonly server: net.Server;
  private readonly sockets = new Set<net.Socket>();
  private readonly forwardedBytes = { listenerToHost: 0, hostToListener: 0 };
  private readonly delayObservations: Record<DelayDirection, DelayObservation> = {
    listenerToHost: { chunks: 0, minScheduledMs: null, maxScheduledMs: null, minObservedMs: null, maxObservedMs: null },
    hostToListener: { chunks: 0, minScheduledMs: null, maxScheduledMs: null, minObservedMs: null, maxObservedMs: null },
  };
  private portValue: number | null = null;

  constructor(
    private readonly targetPort: number,
    private readonly oneWayDelayMs: number,
    private readonly jitterMs = 0,
  ) {
    if (!Number.isFinite(oneWayDelayMs) || oneWayDelayMs < 0 ||
      !Number.isFinite(jitterMs) || jitterMs < 0 || jitterMs > oneWayDelayMs) {
      throw new RangeError('TCP delay must be non-negative and jitter cannot exceed its base delay.');
    }
    this.server = net.createServer((listener) => this.handleConnection(listener));
  }

  get port(): number {
    if (this.portValue === null) throw new Error('The latency proxy has not started.');
    return this.portValue;
  }

  get traffic(): Readonly<typeof this.forwardedBytes> {
    return { ...this.forwardedBytes };
  }

  get observedDelays(): Readonly<Record<DelayDirection, Readonly<DelayObservation>>> {
    return {
      listenerToHost: { ...this.delayObservations.listenerToHost },
      hostToListener: { ...this.delayObservations.hostToListener },
    };
  }

  async start(): Promise<number> {
    await new Promise<void>((resolve, reject) => {
      const onError = (error: Error) => {
        this.server.off('listening', onListening);
        reject(error);
      };
      const onListening = () => {
        this.server.off('error', onError);
        const address = this.server.address();
        if (!address || typeof address === 'string') {
          reject(new Error('The latency proxy did not receive a TCP port.'));
          return;
        }
        this.portValue = address.port;
        resolve();
      };
      this.server.once('error', onError);
      this.server.once('listening', onListening);
      this.server.listen(0, '127.0.0.1');
    });
    return this.port;
  }

  async close(): Promise<void> {
    if (!this.server.listening) return;
    for (const socket of this.sockets) socket.destroy();
    await new Promise<void>((resolve, reject) => {
      this.server.close((error) => error ? reject(error) : resolve());
    });
    this.portValue = null;
  }

  private handleConnection(listener: net.Socket): void {
    const host = net.createConnection({ host: '127.0.0.1', port: this.targetPort });
    this.sockets.add(listener);
    this.sockets.add(host);
    listener.setNoDelay(true);
    host.setNoDelay(true);
    listener.once('close', () => this.sockets.delete(listener));
    host.once('close', () => this.sockets.delete(host));

    const closePair = () => {
      if (!listener.destroyed) listener.destroy();
      if (!host.destroyed) host.destroy();
    };
    listener.once('error', closePair);
    host.once('error', closePair);
    host.once('connect', () => {
      void pipeline(listener, this.addDelay('listenerToHost'), host).catch(closePair);
      void pipeline(host, this.addDelay('hostToListener'), listener).catch(closePair);
    });
  }

  private addDelay(direction: DelayDirection): Transform {
    const proxy = this;
    let chunkIndex = 0;
    let pendingTimer: ReturnType<typeof setTimeout> | null = null;
    const transform = new Transform({
      transform(this: Transform, chunk: Buffer, _encoding: BufferEncoding, callback: TransformCallback) {
        const delayedChunk = Buffer.from(chunk);
        const jitterMultiplier = jitterPattern[chunkIndex % jitterPattern.length] ?? 0;
        chunkIndex += 1;
        const scheduledDelayMs = proxy.oneWayDelayMs + proxy.jitterMs * jitterMultiplier;
        const startedAt = performance.now();
        pendingTimer = setTimeout(() => {
          pendingTimer = null;
          if (this.destroyed) return;
          proxy.recordDelay(direction, scheduledDelayMs, performance.now() - startedAt);
          callback(null, delayedChunk);
        }, scheduledDelayMs);
      },
    });
    transform.once('close', () => {
      if (pendingTimer !== null) clearTimeout(pendingTimer);
      pendingTimer = null;
    });
    transform.on('data', (chunk: Buffer) => {
      this.forwardedBytes[direction] += chunk.byteLength;
    });
    return transform;
  }

  private recordDelay(direction: DelayDirection, scheduledMs: number, observedMs: number): void {
    const observation = this.delayObservations[direction];
    observation.chunks += 1;
    observation.minScheduledMs = observation.minScheduledMs === null ? scheduledMs : Math.min(observation.minScheduledMs, scheduledMs);
    observation.maxScheduledMs = observation.maxScheduledMs === null ? scheduledMs : Math.max(observation.maxScheduledMs, scheduledMs);
    observation.minObservedMs = observation.minObservedMs === null ? observedMs : Math.min(observation.minObservedMs, observedMs);
    observation.maxObservedMs = observation.maxObservedMs === null ? observedMs : Math.max(observation.maxObservedMs, observedMs);
  }
}
