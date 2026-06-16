import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  base: './',
  build: {
    cssMinify: 'esbuild',
    outDir: 'build',
  },
  plugins: [react()],
});
