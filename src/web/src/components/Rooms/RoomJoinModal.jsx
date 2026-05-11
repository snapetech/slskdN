import './Rooms.css';
import * as rooms from '../../lib/rooms';
import React, { useEffect, useMemo, useState } from 'react';
import { toast } from 'react-toastify';
import {
  Button,
  Dimmer,
  Header,
  Icon,
  Input,
  Loader,
  Modal,
  Popup,
  Segment,
  Table,
} from 'semantic-ui-react';

const asArray = (value) => (Array.isArray(value) ? value : []);
const normalizeRoom = (room) => {
  if (
    !room ||
    typeof room !== 'object' ||
    Array.isArray(room) ||
    typeof room.name !== 'string' ||
    !room.name
  ) {
    return undefined;
  }

  return {
    ...room,
    isModerated: room.isModerated === true,
    isOwned: room.isOwned === true,
    isPrivate: room.isPrivate === true,
    userCount: Number.isFinite(room.userCount) ? room.userCount : 0,
  };
};

const RoomJoinModal = ({ joinRoom: parentJoinRoom, ...modalOptions }) => {
  const [open, setOpen] = useState(false);
  const [available, setAvailable] = useState([]);
  const [selected, setSelected] = useState(undefined);
  const [sortBy, setSortBy] = useState('name');
  const [sortOrder, setSortOrder] = useState('desc');
  const [filter, setFilter] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const getAvailableRooms = async () => {
      setLoading(true);
      try {
        const availableResult = await rooms.getAvailable();
        setAvailable(
          asArray(availableResult).map(normalizeRoom).filter(Boolean),
        );
      } catch {
        toast.error('Failed to load room list');
        setAvailable([]);
      } finally {
        setLoading(false);
      }
    };

    if (open) getAvailableRooms();
  }, [open]);

  const sortedAvailable = useMemo(() => {
    const sorted = [...available].filter((room) =>
      room.name.toLowerCase().includes(filter.toLowerCase()),
    );

    sorted.sort((a, b) => {
      if (sortOrder === 'asc') {
        if (typeof a[sortBy] === 'string') {
          return b[sortBy].localeCompare(a[sortBy]);
        }

        return a[sortBy] - b[sortBy];
      }

      if (typeof a[sortBy] === 'string') {
        return a[sortBy].localeCompare(b[sortBy]);
      }

      return b[sortBy] - a[sortBy];
    });

    return sorted;
  }, [available, filter, sortBy, sortOrder]);

  const close = () => {
    setAvailable([]);
    setSelected(undefined);
    setSortBy('name');
    setSortOrder('desc');
    setFilter('');
    setOpen(false);
  };

  const joinRoom = async () => {
    await parentJoinRoom(selected);
    close();
  };

  const isSelected = (room) => selected === room.name;
  const changeSort = (nextSortBy) => {
    if (sortBy === nextSortBy) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
      return;
    }

    setSortBy(nextSortBy);
    setSortOrder('asc');
  };

  return (
    <>
      <Popup
        content="Browse available Soulseek rooms and join the selected room."
        trigger={
          <Button
            color="green"
            icon
            onClick={() => setOpen(true)}
            title="Join Room"
          >
            <Icon name="sign in" />
            Join Room
          </Button>
        }
      />
      <Modal
        className="join-room-modal"
        onClose={() => close()}
        open={open}
        {...modalOptions}
      >
        <Header>
          <Icon name="comments" />
          <Modal.Content>Join Room</Modal.Content>
        </Header>
        <Modal.Content scrolling>
          {loading ? (
            <Dimmer
              active
              inverted
            >
              <Loader
                content="Loading Room List"
                inverted
              />
            </Dimmer>
          ) : (
            <>
              <Input
                fluid
                icon="filter"
                onChange={(_, event) => setFilter(event.value)}
                placeholder="Room Filter"
              />
              {!filter.trim() ? (
                <Segment
                  placeholder
                  textAlign="center"
                >
                  <Header icon>
                    <Icon name="search" />
                    Type to search rooms
                  </Header>
                  <p>{available.length.toLocaleString()} rooms available. Enter a name above to filter.</p>
                </Segment>
              ) : sortedAvailable.length === 0 ? (
                <Segment
                  placeholder
                  textAlign="center"
                >
                  <Header icon>
                    <Icon name="comments outline" />
                    No rooms match &ldquo;{filter}&rdquo;
                  </Header>
                  <p>Try a different search term.</p>
                </Segment>
              ) : (
                <Table
                  celled
                  selectable
                >
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell onClick={() => changeSort('name')}>
                        Name
                        <Icon
                          link={sortBy === 'name'}
                          name={
                            sortBy === 'name' &&
                            (sortOrder === 'asc' ? 'chevron up' : 'chevron down')
                          }
                        />
                      </Table.HeaderCell>
                      <Table.HeaderCell onClick={() => changeSort('userCount')}>
                        Users
                        <Icon
                          link={sortBy === 'userCount'}
                          name={
                            sortBy === 'userCount' &&
                            (sortOrder === 'asc' ? 'chevron up' : 'chevron down')
                          }
                        />
                      </Table.HeaderCell>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {sortedAvailable.map((room) => (
                      <Table.Row
                        key={room.name}
                        onClick={() => setSelected(room.name)}
                        style={isSelected(room) ? { fontWeight: 'bold' } : {}}
                      >
                        <Table.Cell>
                          {isSelected(room) && (
                            <Icon
                              color="green"
                              name="check"
                            />
                          )}
                          {room.isPrivate && <Icon name="lock" />}
                          {room.isOwned && <Icon name="chess queen" />}
                          {room.isModerated && <Icon name="gavel" />}
                          {room.name}
                        </Table.Cell>
                        <Table.Cell>{room.userCount}</Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
              )}
            </>
          )}
        </Modal.Content>
        <Modal.Actions>
          <Button onClick={() => close()}>Cancel</Button>
          <Button
            disabled={!selected}
            onClick={() => joinRoom()}
            positive
          >
            Join
          </Button>
        </Modal.Actions>
      </Modal>
    </>
  );
};

export default RoomJoinModal;
