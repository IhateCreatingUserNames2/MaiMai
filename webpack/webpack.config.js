const path = require('path');
const webpack = require('webpack');

module.exports = {
  mode: 'production',
  entry: './src/index.ts',
  output: {
    filename: 'langgraph.bundle.mjs',
    path: path.resolve(__dirname, 'dist'),
    libraryTarget: 'module',
  },
  experiments: { outputModule: true },
  target: 'web',
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: {
            presets: ['@babel/preset-env', '@babel/preset-typescript'],
          },
        },
      },
    ],
  },
  resolve: {
    extensions: ['.ts', '.js'],
    alias: {
      'web-streams-polyfill/ponyfill': path.resolve(__dirname, 'node_modules/web-streams-polyfill/dist/ponyfill.mjs')
    },
    fallback: {
      'node:async_hooks': false,
      fs: false,
      net: false,
      tls: false,
    },
  },
  plugins: [
    new webpack.DefinePlugin({ 'process.env.NODE_ENV': JSON.stringify('production') }),
    new webpack.ProvidePlugin({
      ReadableStream: ['web-streams-polyfill/ponyfill', 'ReadableStream'],
      WritableStream: ['web-streams-polyfill/ponyfill', 'WritableStream'],
      TransformStream: ['web-streams-polyfill/ponyfill', 'TransformStream'],
      ByteLengthQueuingStrategy: ['web-streams-polyfill/ponyfill', 'ByteLengthQueuingStrategy'],
      CountQueuingStrategy: ['web-streams-polyfill/ponyfill', 'CountQueuingStrategy'],
    }),
  ],
};