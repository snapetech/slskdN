// <copyright file="RadioDirectory.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React, { useEffect, useState } from 'react';
import { Button, Icon, List, Message, Modal, Popup } from 'semantic-ui-react';
import * as listeningParty from '../../lib/listeningParty';

const RadioDirectory = ({ onClose, onPlay }) => {
  const [parties, setParties] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    listeningParty.getPartyDirectory().then((entries) => {
      if (!cancelled) setParties(entries);
    }).catch(() => {
      if (!cancelled) setError('Listed radio could not load. Refresh to try again.');
    }).finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => { cancelled = true; };
  }, [refresh]);

  return (
    <Modal className="player-browser-modal" closeOnDimmerClick onClose={onClose} open size="small">
      <Modal.Header>Listed radio</Modal.Header>
      <Modal.Content scrolling>
        <p>Play a host's current track snapshot. Rejoin for later track changes. Refresh checks the directory only when you request it.</p>
        <Popup content="Refresh the listed snapshots to see current host streaming permissions. Playback starts only when you choose a track." trigger={
          <Button aria-label="Refresh listed radio" disabled={loading} icon loading={loading} onClick={() => setRefresh((value) => value + 1)} size="small">
            <Icon name="refresh" />
            Refresh
          </Button>
        } />
        {error ? <Message negative role="alert">{error}</Message> : null}
        {loading ? <Message role="status">Loading listed radio…</Message> : null}
        {!loading && !error && parties.length === 0 ? <Message role="status">No radio broadcasts are listed right now.</Message> : null}
        <List divided relaxed>
          {parties.map((party) => {
            const playable = party.allowMeshStreaming && Boolean(party.transportUsername && party.streamTicket);
            return (
              <List.Item key={party.partyId}>
                <List.Content floated="right">
                  <Popup content={playable ? "Play this host's current track. Remote playback needs an existing mesh connection to the host." : party.allowMeshStreaming ? 'This host needs to publish a current snapshot with radio transport support.' : 'This broadcast shares metadata only; its host has not enabled streaming.'} trigger={
                    <Button aria-label={`Play ${party.title || party.contentId} from listed radio`} disabled={!playable} icon="play" onClick={() => { onPlay(party); onClose(); }} size="small" />
                  } />
                </List.Content>
                <List.Content>
                  <List.Header>{party.title || party.contentId}</List.Header>
                  <List.Description>{party.artist || party.hostPeerId} · {playable ? 'Host enables streaming' : party.allowMeshStreaming ? 'Host update required' : 'Metadata only'}</List.Description>
                </List.Content>
              </List.Item>
            );
          })}
        </List>
      </Modal.Content>
      <Modal.Actions>
        <Popup content="Close the directory and return to the player." trigger={<Button onClick={onClose}>Done</Button>} />
      </Modal.Actions>
    </Modal>
  );
};

export default RadioDirectory;
