// @vitest-environment node
import { describe, expect, it } from 'vitest';
import { SoulseekPrivateMessageIdMapper } from '../e2e/harness/SoulseekPrivateMessageIdProxy';

const privateMessageCode = 22;
const acknowledgementCode = 23;

function frame(code, id) {
  const buffer = Buffer.alloc(12);
  buffer.writeUInt32LE(8, 0);
  buffer.writeUInt32LE(code, 4);
  buffer.writeInt32LE(id, 8);
  return buffer;
}

describe('Soulseek private message ID mapper', () => {
  it('maps fragmented fixture message IDs and returns the original ID in acknowledgements', () => {
    const mapper = new SoulseekPrivateMessageIdMapper();
    const incoming = frame(privateMessageCode, -74);

    expect(mapper.push('serverToClient', incoming.subarray(0, 3))).toHaveLength(0);
    expect(mapper.push('serverToClient', incoming.subarray(3, 9))).toHaveLength(0);
    const delivered = mapper.push('serverToClient', incoming.subarray(9));

    expect(delivered).toHaveLength(incoming.length);
    expect(delivered.readInt32LE(8)).not.toBe(-74);
    expect(delivered.readInt32LE(8)).toBeGreaterThan(0);

    const acknowledgement = frame(acknowledgementCode, delivered.readInt32LE(8));
    const forwarded = Buffer.concat([
      mapper.push('clientToServer', acknowledgement.subarray(0, 5)),
      mapper.push('clientToServer', acknowledgement.subarray(5)),
    ]);
    expect(forwarded.readInt32LE(8)).toBe(-74);
    mapper.finish('serverToClient');
    mapper.finish('clientToServer');
  });

  it('preserves unrelated and already-valid private-message frames', () => {
    const mapper = new SoulseekPrivateMessageIdMapper();
    const statusFrame = frame(1, 20);
    const positivePrivateMessage = frame(privateMessageCode, 42);

    expect(mapper.push('serverToClient', statusFrame)).toEqual(statusFrame);
    expect(mapper.push('serverToClient', positivePrivateMessage)).toEqual(positivePrivateMessage);
    expect(mapper.push('clientToServer', frame(acknowledgementCode, 42))).toEqual(frame(acknowledgementCode, 42));
    mapper.finish('serverToClient');
    mapper.finish('clientToServer');
  });

  it('rejects malformed or incomplete TCP frames', () => {
    const malformed = new SoulseekPrivateMessageIdMapper();
    expect(() => malformed.push('serverToClient', Buffer.from([3, 0, 0, 0])))
      .toThrow(/Invalid Soulseek frame/);

    const incomplete = new SoulseekPrivateMessageIdMapper();
    incomplete.push('serverToClient', Buffer.from([8, 0, 0, 0, 22, 0]));
    expect(() => incomplete.finish('serverToClient')).toThrow(/incomplete protocol frame/);
  });
});
