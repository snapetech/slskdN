export const contentExamples = {
  audio: {
    album: {
      content: 'content:audio:album:mb-67890',
      external: 'mb:release:67890',
    },
    artist: {
      content: 'content:audio:artist:mb-abc123',
      external: 'mb:artist:abc123',
    },
    track: {
      content: 'content:audio:track:mb-12345',
      external: 'mb:recording:12345',
    },
  },
  image: {
    artwork: {
      content: 'content:image:artwork:discogs-11111',
      external: 'discogs:release:11111',
    },
    photo: {
      content: 'content:image:photo:flickr-67890',
      external: 'flickr:photo:67890',
    },
  },
  video: {
    movie: {
      content: 'content:video:movie:imdb-tt0111161',
      external: 'imdb:tt0111161',
    },
    series: {
      content: 'content:video:series:tvdb-12345',
      external: 'tvdb:series:12345',
    },
  },
};

export const podWorkflowSections = [
  {
    description: 'Publish or retrieve pod metadata through DHT workflows.',
    href: '#podcore-dht-publishing',
    label: 'DHT Publishing',
    risk: 'Publishes metadata',
  },
  {
    description: 'Manage signed membership records, bans, and roles.',
    href: '#pod-membership-management',
    label: 'Membership',
    risk: 'Publishes membership state',
  },
  {
    description: 'Verify membership, message authenticity, and role claims.',
    href: '#pod-membership-verification',
    label: 'Verification',
    risk: 'Read-only checks',
  },
  {
    description: 'Find listed pods by name, tag, content, or registry scan.',
    href: '#pod-discovery',
    label: 'Discovery',
    risk: 'Read-only unless registering',
  },
  {
    description: 'Request, accept, leave, and review pod membership flows.',
    href: '#pod-join-leave',
    label: 'Join/Leave',
    risk: 'Publishes signed membership events',
  },
  {
    description: 'Route messages, target peers, and review deduplication state.',
    href: '#pod-message-routing',
    label: 'Routing',
    risk: 'Sends pod messages',
  },
  {
    description: 'Search, count, clean up, rebuild, and vacuum pod messages.',
    href: '#pod-message-storage',
    label: 'Storage',
    risk: 'Local storage operations',
  },
  {
    description: 'Synchronize missed messages and last-seen timestamps.',
    href: '#pod-message-backfill',
    label: 'Backfill',
    risk: 'Syncs pod state',
  },
  {
    description: 'Create, inspect, update, and delete pod channels.',
    href: '#pod-channel-management',
    label: 'Channels',
    risk: 'Mutates pod structure',
  },
  {
    description: 'Search content and create content-linked pods.',
    href: '#pod-content-linking',
    label: 'Content Linking',
    risk: 'Can create pods',
  },
  {
    description: 'Publish, aggregate, and review pod opinions.',
    href: '#pod-opinion-management',
    label: 'Opinions',
    risk: 'Publishes opinion data',
  },
  {
    description: 'Sign messages, verify signatures, and generate key pairs.',
    href: '#pod-message-signing',
    label: 'Signing',
    risk: 'Handles key material',
  },
];

export const podWorkflowFilterOptions = [
  { key: 'all', text: 'Show all pod workflows', value: 'all' },
  ...podWorkflowSections.map((section) => ({
    key: section.href,
    text: section.label,
    value: section.href.slice(1),
  })),
];
