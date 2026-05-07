import MessagingV2 from './MessagingV2';
import React from 'react';

export const encodePodTarget = (podId, channelId) => `${podId}\u001f${channelId}`;

export const decodePodTarget = (target) => {
  const [podId, channelId] = `${target || ''}`.split('\u001f');
  return { channelId, podId };
};

export const channelLabel = (channel) =>
  [channel.podName, channel.channelName || channel.channelId]
    .filter(Boolean)
    .join(' / ');

export const asArray = (value) => (Array.isArray(value) ? value : []);

const normalizeConversationName = (value) => `${value || ''}`.trim().toLowerCase();

export const isPodDirectChannel = (channel) => {
  const channelKind = normalizeConversationName(channel.channelKind);
  const channelName = normalizeConversationName(
    channel.channelName || channel.channelId,
  );

  return (
    channelKind === 'direct' ||
    channelName === 'dm' ||
    channelName === 'direct' ||
    channelName === 'direct message'
  );
};

const Messaging = ({ initialKind = 'mixed', state }) => (
  <MessagingV2 initialKind={initialKind} state={state} />
);

export default Messaging;
