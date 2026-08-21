import { usePlayer } from './PlayerContext';
import React from 'react';
import { Button, Icon, Popup } from 'semantic-ui-react';

const PlayCollectionItemButton = ({ item, size = 'small' }) => {
  const { playItem, playerVisible } = usePlayer();

  if (!playerVisible) return null;

  return (
    <Popup
      content="Play this item through the local stream endpoint and update your now-playing status."
      trigger={
        <Button
          data-testid="player-play-item"
          icon
          onClick={() => playItem(item, { replaceQueue: true })}
          size={size}
        >
          <Icon name="play" />
        </Button>
      }
    />
  );
};

export default PlayCollectionItemButton;
