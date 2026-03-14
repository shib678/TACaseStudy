const HtmlWebpackPlugin = require("html-webpack-plugin");
const { ModuleFederationPlugin } = require("webpack").container;
const webpack = require("webpack");
const path = require("path");
const deps = require("./package.json").dependencies;

module.exports = {
  entry: "./src/bootstrap.tsx",
  output: {
    publicPath: "http://localhost:3001/",
    path: path.resolve(__dirname, "dist"),
    filename: "[name].[contenthash].js",
    clean: true,
  },
  resolve: {
    extensions: [".tsx", ".ts", ".js"],
  },
  module: {
    rules: [
      {
        test: /\.(ts|tsx|js|jsx)$/,
        exclude: /node_modules/,
        use: {
          loader: "babel-loader",
          options: {
            presets: [
              "@babel/preset-env",
              ["@babel/preset-react", { runtime: "automatic" }],
              "@babel/preset-typescript",
            ],
          },
        },
      },
      { test: /\.css$/, use: ["style-loader", "css-loader"] },
    ],
  },
  plugins: [
    new webpack.DefinePlugin({
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'development'),
      'process.env.REACT_APP_API_BASE_URL': JSON.stringify(process.env.REACT_APP_API_BASE_URL || ''),
    }),
    new ModuleFederationPlugin({
      name: "mfeUpload",
      filename: "remoteEntry.js",
      exposes: {
        "./App": "./src/App",
        "./uploadSlice": "./src/store/uploadSlice"
      },
      shared: {
        react: { singleton: true, requiredVersion: deps.react },
        "react-dom": { singleton: true, requiredVersion: deps["react-dom"] },
        "react-router-dom": { singleton: true, requiredVersion: deps["react-router-dom"] },
        "@reduxjs/toolkit": { singleton: true, requiredVersion: deps["@reduxjs/toolkit"] },
        "react-redux": { singleton: true, requiredVersion: deps["react-redux"] },
        "@mui/material": { singleton: true, requiredVersion: deps["@mui/material"] },
        "@emotion/react": { singleton: true, requiredVersion: deps["@emotion/react"] },
        "@emotion/styled": { singleton: true, requiredVersion: deps["@emotion/styled"] },
      },
    }),
    new HtmlWebpackPlugin({ template: "./public/index.html" }),
  ],
  devServer: {
    port: 3001,
    historyApiFallback: true,
    headers: { "Access-Control-Allow-Origin": "*" },
  },
};
