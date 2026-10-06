import React from 'react';
import { useMediaQuery } from 'react-responsive';
import { Button, Icon, Popup } from 'semantic-ui-react';

const ShrinkableButton = ({
  children,
  icon,
  loading,
  mediaQuery,
  tooltip,
  'aria-label': ariaLabel,
  ...rest
}) => {
  const shouldShrink = useMediaQuery({ query: mediaQuery });
  const textLabel = typeof children === 'string' ? children.trim() : undefined;
  const accessibleLabel = ariaLabel || textLabel ||
    (typeof tooltip === 'string' ? tooltip : undefined);
  const button = (
    <Button
      aria-label={accessibleLabel}
      icon={shouldShrink || undefined}
      title={tooltip}
      {...rest}
    >
      <Icon
        loading={loading}
        name={icon}
      />
      {!shouldShrink && children}
    </Button>
  );

  if (!tooltip) return button;

  return (
    <Popup
      content={tooltip}
      trigger={rest.disabled ? <span>{button}</span> : button}
    />
  );
};

export default ShrinkableButton;
