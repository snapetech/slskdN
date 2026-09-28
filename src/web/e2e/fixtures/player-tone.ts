// <copyright file="player-tone.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

// Generated PCM uses no personal files, downloaded media or remote peers.
export function makeTone(seconds = 40): Buffer {
  const sampleRate = 22050;
  const samples = sampleRate * seconds;
  const wave = Buffer.alloc(44 + samples * 2);
  wave.write('RIFF', 0);
  wave.writeUInt32LE(wave.length - 8, 4);
  wave.write('WAVEfmt ', 8);
  wave.writeUInt32LE(16, 16);
  wave.writeUInt16LE(1, 20);
  wave.writeUInt16LE(1, 22);
  wave.writeUInt32LE(sampleRate, 24);
  wave.writeUInt32LE(sampleRate * 2, 28);
  wave.writeUInt16LE(2, 32);
  wave.writeUInt16LE(16, 34);
  wave.write('data', 36);
  wave.writeUInt32LE(samples * 2, 40);
  for (let index = 0; index < samples; index++) {
    wave.writeInt16LE(Math.round(Math.sin(index * 2 * Math.PI * 440 / sampleRate) * 1600), 44 + index * 2);
  }
  return wave;
}
