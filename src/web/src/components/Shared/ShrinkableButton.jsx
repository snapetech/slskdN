import React from 'react';
import { useMediaQuery } from 'react-responsive';
import { Button, Icon, Popup } from 'semantic-ui-react';

const ShrinkableButton = ({
  children,
  icon,
  loading,
  mediaQuery,
  tooltip,
  ...rest
}) => {
  const shouldShrink = useMediaQuery({ query: mediaQuery });
  const description = tooltip || children;

  if (tooltip) {
    const button = (
      <Button
        aria-label={shouldShrink ? tooltip : rest['aria-label']}
        icon={shouldShrink}
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

    return (
      <Popup
        content={tooltip}
        trigger={rest.disabled ? <span>{button}</span> : button}
      />
    );
  }

  if (!shouldShrink) {
    return (
      <Button {...rest}>
        <Icon
          loading={loading}
          name={icon}
        />
        {children}
      </Button>
    );
  }

  const button = (
    <Button
      icon
      {...rest}
    >
      <Icon
        loading={loading}
        name={icon}
      />
    </Button>
  );

  return (
    <Popup
      content={description}
      trigger={rest.disabled ? <span>{button}</span> : button}
    />
  );
};

export default ShrinkableButton;
