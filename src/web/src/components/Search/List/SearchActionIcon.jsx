import React from 'react';
import { Icon } from 'semantic-ui-react';
import TooltipButton from '../../Shared/TooltipButton';

const SearchActionIcon = ({ loading, onRemove, onStop, search, ...props }) => {
  if (loading) {
    return (
      <Icon
        loading
        name="spinner"
        {...props}
      />
    );
  }

  if (search.state.includes('Completed')) {
    return (
      <TooltipButton
        aria-label={`Delete completed search ${search.searchText || search.id}`}
        basic
        compact
        icon
        onClick={() => onRemove()}
        tooltip={`Delete the completed search ${search.searchText || search.id} and its response history.`}
        {...props}
      >
        <Icon color="red" name="trash alternate" />
      </TooltipButton>
    );
  }

  return (
    <TooltipButton
      aria-label={`Stop search ${search.searchText || search.id}`}
      basic
      compact
      icon
      onClick={() => onStop()}
      tooltip={`Stop ${search.searchText || search.id} so it no longer requests responses from Soulseek peers.`}
      {...props}
    >
      <Icon color="red" name="stop circle" />
    </TooltipButton>
  );
};

export default SearchActionIcon;
