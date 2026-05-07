const asArray = (value) => (Array.isArray(value) ? value : []);

const isDirectory = (directory) =>
  directory &&
  typeof directory === 'object' &&
  !Array.isArray(directory) &&
  typeof directory.name === 'string';

const buildDirectoryTree = ({ directories, separator }) => {
  const validDirectories = asArray(directories).filter(isDirectory);

  if (validDirectories.length === 0) {
    return [];
  }

  const effectiveSeparator = separator || '\\';
  const nodesByName = new Map();
  const roots = [];

  for (const directory of validDirectories) {
    nodesByName.set(directory.name, { ...directory, children: [] });
  }

  for (const node of nodesByName.values()) {
    const parts = node.name.split(effectiveSeparator);
    const parentName =
      parts.length > 1 ? parts.slice(0, -1).join(effectiveSeparator) : '';
    const parent = parentName ? nodesByName.get(parentName) : null;

    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }

  return roots;
};

self.onmessage = ({ data }) => {
  try {
    self.postMessage({
      id: data.id,
      tree: buildDirectoryTree(data),
    });
  } catch (error) {
    self.postMessage({
      error: error instanceof Error ? error.message : 'Failed to build tree',
      id: data.id,
    });
  }
};
