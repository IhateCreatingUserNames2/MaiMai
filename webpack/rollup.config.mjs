import resolve from 'rollup-plugin-node-resolve';
import commonjs from 'rollup-plugin-commonjs';
import json from 'rollup-plugin-json';

export default {
  input: 'src/index.js',
  output: {
    file: 'dist/langgraph.bundle.mjs',
    format: 'esm',
  },
  plugins: [
    resolve({
      browser: true,
      preferBuiltins: false,
    }),
    commonjs(),
    json(),
  ],
  onwarn: function (warning, warn) {
    if (warning.code === 'CIRCULAR_DEPENDENCY') {
      // Ignore circular dependency warnings if they are not critical
      return;
    }
    warn(warning);
  },
};
