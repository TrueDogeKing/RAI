'use strict';

const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = (env, argv) => {
    const produkcja = argv.mode === 'production';

    return {
        mode: produkcja ? 'production' : 'development',
        entry: './src/index.js',
        output: {
            path: path.resolve(__dirname, 'dist'),
            filename: produkcja ? 'bundle.[contenthash].js' : 'bundle.js',
            clean: true
        },
        // source mapy: pelny plik .map w produkcji, szybszy wariant w trybie dev
        devtool: produkcja ? 'source-map' : 'eval-source-map',
        module: {
            rules: [
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader',
                        options: {
                            presets: [['@babel/preset-env', { targets: 'ie 11' }]]
                        }
                    }
                },
                {
                    test: /\.css$/,
                    use: ['style-loader', 'css-loader']
                }
            ]
        },
        plugins: [
            new HtmlWebpackPlugin({
                template: './public/index.html'
            })
        ],
        devServer: {
            port: 8080,
            hot: true,
            open: false
        }
    };
};
