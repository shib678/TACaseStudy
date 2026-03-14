const HtmlWebpackPlugin = require("html-webpack-plugin");
const { ModuleFederationPlugin, DefinePlugin } = require("webpack").container;
const webpack = require("webpack");
const path = require("path");
const deps = require("./package.json").dependencies;

module.exports = {
  entry: "./src/bootstrap.tsx",
  output: {
    publicPath: "http://localhost:3000/",
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
      {
        test: /\.css$/,
        use: ["style-loader", "css-loader"],
      },
    ],
  },
  plugins: [
    new webpack.DefinePlugin({
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'development'),
    }),
    new ModuleFederationPlugin({
      name: "shell",
      remotes: {
        mfeUpload: "mfeUpload@http://localhost:3001/remoteEntry.js",
        mfeSearch: "mfeSearch@http://localhost:3002/remoteEntry.js",
      },
      shared: {
        react: {
          singleton: true,
          requiredVersion: deps.react,
          eager: true,
        },
        "react-dom": {
          singleton: true,
          requiredVersion: deps["react-dom"],
          eager: true,
        },
        "react-router-dom": {
          singleton: true,
          requiredVersion: deps["react-router-dom"],
          eager: true,
        },
        "@reduxjs/toolkit": {
          singleton: true,
          requiredVersion: deps["@reduxjs/toolkit"],
          eager: true,
        },
        "react-redux": {
          singleton: true,
          requiredVersion: deps["react-redux"],
          eager: true,
        },
        "@mui/material": {
          singleton: true,
          requiredVersion: deps["@mui/material"],
          eager: true,
        },
        "@emotion/react": {
          singleton: true,
          requiredVersion: deps["@emotion/react"],
          eager: true,
        },
        "@emotion/styled": {
          singleton: true,
          requiredVersion: deps["@emotion/styled"],
          eager: true,
        },
      },
    }),
    new HtmlWebpackPlugin({
      template: "./public/index.html",
    }),
  ],
  devServer: {
    port: 3000,
    historyApiFallback: true,
    headers: {
      "Access-Control-Allow-Origin": "*",
    },
  },
};
