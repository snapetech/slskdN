import { THEME_PALETTES } from '../lib/themes';
import React from 'react';
import { Icon, Menu, Popup } from 'semantic-ui-react';

const THEME_OPTIONS = [
  { key: 'slskdn', text: 'slskdN', value: 'slskdn' },
  { key: 'classic-dark', text: 'Classic Dark', value: 'classic-dark' },
  { key: 'light', text: 'Light', value: 'light' },
];

const ThemeMenu = ({
  closeThemeMenu,
  onSetPalette,
  onSetTheme,
  openThemeMenu,
  open,
  palette,
  semanticTheme,
  theme,
}) => (
  <Popup
    basic
    className="theme-picker-popup"
    on="click"
    onClose={closeThemeMenu}
    onOpen={openThemeMenu}
    open={open}
    pinned
    position="bottom right"
    trigger={(
      <Menu.Item
        aria-expanded={open}
        aria-haspopup="menu"
        aria-label="Choose the web UI color theme"
        className={`theme-menu ${open ? 'visible' : ''}`}
        data-testid="theme-menu"
        title="Choose the web UI color theme"
      >
        <Icon name="paint brush" />
        <span className="theme-menu-label">Theme</span>
      </Menu.Item>
    )}
  >
    <Menu
      className="theme-picker-menu"
      vertical
    >
      {THEME_OPTIONS.map((option) => {
        const description = `Use the ${option.text} theme for the web UI.`;

        return (
          <Popup
            content={description}
            key={option.key}
            trigger={(
              <Menu.Item
                active={theme === option.value}
                aria-label={description}
                data-testid={`theme-option-${option.value}`}
                onClick={() => onSetTheme(option.value)}
                title={description}
              >
                <Icon name="theme" />
                {option.text}
              </Menu.Item>
            )}
          />
        );
      })}
      {semanticTheme !== 'light' && (
        <>
          <Menu.Item
            style={{
              cursor: 'default',
              fontSize: '0.75rem',
              fontWeight: 600,
              letterSpacing: '0.04em',
              opacity: 0.55,
              textTransform: 'uppercase',
            }}
          >
            Palette
          </Menu.Item>
          <div className="theme-palette-grid">
            {THEME_PALETTES.map((paletteOption) => {
              const description =
                `Apply the ${paletteOption.name} palette to the dark theme.`;

              return (
                <Popup
                  content={description}
                  key={paletteOption.id}
                  trigger={(
                    <button
                      aria-label={description}
                      className={`theme-palette-swatch ${
                        palette === paletteOption.id ? 'active' : ''
                      }`}
                      onClick={() => onSetPalette(paletteOption.id)}
                      title={description}
                      type="button"
                    >
                      <span className="theme-palette-swatch-dots">
                        {paletteOption.swatches.map((swatch) => (
                          <span
                            className="theme-palette-dot"
                            key={`${paletteOption.id}-${swatch}`}
                            style={{ backgroundColor: swatch }}
                          />
                        ))}
                      </span>
                      <span className="theme-palette-swatch-name">
                        {paletteOption.name}
                      </span>
                    </button>
                  )}
                />
              );
            })}
          </div>
          {palette && (
            <Popup
              content="Restore the default colors for the dark theme."
              trigger={(
                <Menu.Item
                  aria-label="Restore the default colors for the dark theme."
                  onClick={() => onSetPalette(null)}
                  title="Restore the default colors for the dark theme."
                  style={{ fontSize: '0.8rem', opacity: 0.65 }}
                >
                  <Icon name="undo" />
                  Reset palette
                </Menu.Item>
              )}
            />
          )}
        </>
      )}
    </Menu>
  </Popup>
);

export default ThemeMenu;
