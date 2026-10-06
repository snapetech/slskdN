import ShrinkableButton from './ShrinkableButton';
import React from 'react';
import { Button, Dropdown, Popup } from 'semantic-ui-react';

const ShrinkableDropdownButton = ({
  children,
  color,
  disabled,
  hidden,
  icon,
  loading,
  menuLabel,
  mediaQuery,
  onChange,
  onClick,
  options,
  tooltip,
}) => {
  if (hidden) {
    return null;
  }

  const menu = (
    <Dropdown
      aria-label={menuLabel || (typeof children === 'string' ? `${children} options` : undefined)}
      className="button icon"
      disabled={disabled}
      onChange={onChange}
      options={options}
      title={tooltip}
      trigger={null}
    />
  );

  return (
    <Button.Group color={color}>
      <ShrinkableButton
        disabled={disabled}
        icon={icon}
        loading={loading}
        mediaQuery={mediaQuery}
        onClick={onClick}
        tooltip={tooltip}
      >
        {children}
      </ShrinkableButton>
      {tooltip ? (
        <Popup
          content={tooltip}
          trigger={disabled ? <span>{menu}</span> : menu}
        />
      ) : menu}
    </Button.Group>
  );
};

export default ShrinkableDropdownButton;
