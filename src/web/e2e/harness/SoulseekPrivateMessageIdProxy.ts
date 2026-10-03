// <copyright file="SoulseekPrivateMessageIdProxy.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as net from 'node:net';
import { Transform, type TransformCallback } from 'node:stream';
import { pipeline } from 'node:stream/promises';

const serverPrivateMessageCode = 22;
const serverPrivateMessageAcknowledgementCode = 23;
const maximumPayloadLength = 64 * 1024 * 1024;

type FrameDirection = 'clientToServer' | 'serverToClient';

/**
 * Reframes Soulseek TCP messages and gives fixture-generated negative private
 * message IDs a positive ID for the protocol client, mapping acknowledgements
 * back to the fixture's original ID.
 */
export class SoulseekPrivateMessageIdMapper {
  private readonly pending: Record<FrameDirection, Buffer> = {
    clientToServer: Buffer.alloc(0),
    serverToClient: Buffer.alloc(0),
  };

  private readonly fixtureIdsByClientId = new Map<number, number>();

  private readonly clientIdsByFixtureId = new Map<number, number>();

  private readonly clientIdsInUse = new Set<number>();

  private nextClientId = 1_500_000_000;

  push(direction: FrameDirection, chunk: Buffer): Buffer {
    this.pending[direction] = Buffer.concat([this.pending[direction], chunk]);

    const frames: Buffer[] = [];
    while (this.pending[direction].length >= 4) {
      const payloadLength = this.pending[direction].readUInt32LE(0);
      if (payloadLength < 4 || payloadLength > maximumPayloadLength) {
        throw new Error(`Invalid Soulseek frame payload length: ${payloadLength}.`);
      }

      const frameLength = payloadLength + 4;
      if (this.pending[direction].length < frameLength) break;

      const frame = this.pending[direction].subarray(0, frameLength);
      frames.push(this.mapPrivateMessageId(direction, frame));
      this.pending[direction] = this.pending[direction].subarray(frameLength);
    }

    return frames.length === 0 ? Buffer.alloc(0) : Buffer.concat(frames);
  }

  finish(direction: FrameDirection): void {
    if (this.pending[direction].length > 0) {
      throw new Error('Soulseek connection closed with an incomplete protocol frame.');
    }
  }

  private mapPrivateMessageId(direction: FrameDirection, frame: Buffer): Buffer {
    const code = frame.readUInt32LE(4);
    if (direction === 'serverToClient' && code === serverPrivateMessageCode && frame.length >= 12) {
      return this.mapIncomingPrivateMessageId(frame);
    }

    if (direction === 'clientToServer' &&
      code === serverPrivateMessageAcknowledgementCode && frame.length >= 12) {
      return this.mapAcknowledgementId(frame);
    }

    return frame;
  }

  private mapIncomingPrivateMessageId(frame: Buffer): Buffer {
    const fixtureId = frame.readInt32LE(8);
    if (fixtureId >= 0) {
      this.clientIdsInUse.add(fixtureId);
      return frame;
    }

    let clientId = this.clientIdsByFixtureId.get(fixtureId);
    if (clientId === undefined) {
      while (this.clientIdsInUse.has(this.nextClientId)) this.nextClientId += 1;
      clientId = this.nextClientId;
      this.nextClientId += 1;
      this.clientIdsInUse.add(clientId);
      this.fixtureIdsByClientId.set(clientId, fixtureId);
      this.clientIdsByFixtureId.set(fixtureId, clientId);
    }

    const mappedFrame = Buffer.from(frame);
    mappedFrame.writeInt32LE(clientId, 8);
    return mappedFrame;
  }

  private mapAcknowledgementId(frame: Buffer): Buffer {
    const clientId = frame.readInt32LE(8);
    const fixtureId = this.fixtureIdsByClientId.get(clientId);
    if (fixtureId === undefined) return frame;

    const mappedFrame = Buffer.from(frame);
    mappedFrame.writeInt32LE(fixtureId, 8);
    return mappedFrame;
  }
}

/**
 * A loopback TCP proxy for the pinned Soulfind E2E fixture. Production clients
 * keep their non-negative private-message ID validation unchanged.
 */
export class SoulseekPrivateMessageIdProxy {
  private readonly server: net.Server;

  private readonly sockets = new Set<net.Socket>();

  private readonly targetPort: number;

  private portValue: number | null = null;

  constructor(targetPort: number) {
    if (!Number.isInteger(targetPort) || targetPort < 1024 || targetPort > 65535) {
      throw new RangeError('The Soulseek fixture proxy requires a valid loopback target port.');
    }

    this.targetPort = targetPort;
    this.server = net.createServer((client) => this.handleConnection(client));
  }

  get port(): number {
    if (this.portValue === null) throw new Error('The Soulseek fixture proxy has not started.');
    return this.portValue;
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
          reject(new Error('The Soulseek fixture proxy did not receive a TCP port.'));
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

  private handleConnection(client: net.Socket): void {
    const fixture = net.createConnection({ host: '127.0.0.1', port: this.targetPort });
    const mapper = new SoulseekPrivateMessageIdMapper();
    this.sockets.add(client);
    this.sockets.add(fixture);
    client.setNoDelay(true);
    fixture.setNoDelay(true);
    client.once('close', () => this.sockets.delete(client));
    fixture.once('close', () => this.sockets.delete(fixture));

    const closePair = () => {
      if (!client.destroyed) client.destroy();
      if (!fixture.destroyed) fixture.destroy();
    };
    client.once('error', closePair);
    fixture.once('error', closePair);
    fixture.once('connect', () => {
      void pipeline(client, this.mapFrames(mapper, 'clientToServer'), fixture)
        .catch(closePair);
      void pipeline(fixture, this.mapFrames(mapper, 'serverToClient'), client)
        .catch(closePair);
    });
  }

  private mapFrames(mapper: SoulseekPrivateMessageIdMapper, direction: FrameDirection): Transform {
    return new Transform({
      transform(chunk: Buffer, _encoding: BufferEncoding, callback: TransformCallback) {
        try {
          callback(null, mapper.push(direction, chunk));
        } catch (error) {
          callback(error instanceof Error ? error : new Error(String(error)));
        }
      },
      flush(callback: TransformCallback) {
        try {
          mapper.finish(direction);
          callback();
        } catch (error) {
          callback(error instanceof Error ? error : new Error(String(error)));
        }
      },
    });
  }
}
