import assert from 'node:assert/strict';
import test from 'node:test';
import { SoulseekPrivateMessageIdMapper } from './SoulseekPrivateMessageIdProxy.ts';

const privateMessageCode = 22;
const acknowledgementCode = 23;

function frame(code, id) {
  const buffer = Buffer.alloc(12);
  buffer.writeUInt32LE(8, 0);
  buffer.writeUInt32LE(code, 4);
  buffer.writeInt32LE(id, 8);
  return buffer;
}

test('maps fragmented fixture message IDs and returns the original ID in acknowledgements', () => {
  const mapper = new SoulseekPrivateMessageIdMapper();
  const incoming = frame(privateMessageCode, -74);

  assert.equal(mapper.push('serverToClient', incoming.subarray(0, 3)).length, 0);
  assert.equal(mapper.push('serverToClient', incoming.subarray(3, 9)).length, 0);
  const delivered = mapper.push('serverToClient', incoming.subarray(9));

  assert.equal(delivered.length, incoming.length);
  assert.notEqual(delivered.readInt32LE(8), -74);
  assert.ok(delivered.readInt32LE(8) > 0);

  const acknowledgement = frame(acknowledgementCode, delivered.readInt32LE(8));
  const forwarded = Buffer.concat([
    mapper.push('clientToServer', acknowledgement.subarray(0, 5)),
    mapper.push('clientToServer', acknowledgement.subarray(5)),
  ]);
  assert.equal(forwarded.readInt32LE(8), -74);
  mapper.finish('serverToClient');
  mapper.finish('clientToServer');
});

test('preserves unrelated and already-valid private-message frames', () => {
  const mapper = new SoulseekPrivateMessageIdMapper();
  const statusFrame = frame(1, 20);
  const positivePrivateMessage = frame(privateMessageCode, 42);

  assert.deepEqual(mapper.push('serverToClient', statusFrame), statusFrame);
  assert.deepEqual(mapper.push('serverToClient', positivePrivateMessage), positivePrivateMessage);
  assert.deepEqual(mapper.push('clientToServer', frame(acknowledgementCode, 42)), frame(acknowledgementCode, 42));
  mapper.finish('serverToClient');
  mapper.finish('clientToServer');
});

test('rejects malformed or incomplete TCP frames', () => {
  const malformed = new SoulseekPrivateMessageIdMapper();
  assert.throws(() => malformed.push('serverToClient', Buffer.from([3, 0, 0, 0])), /Invalid Soulseek frame/);

  const incomplete = new SoulseekPrivateMessageIdMapper();
  incomplete.push('serverToClient', Buffer.from([8, 0, 0, 0, 22, 0]));
  assert.throws(() => incomplete.finish('serverToClient'), /incomplete protocol frame/);
});
