# ScrapRenamer

## Basic Information

* Software: ScrapRenamer
* Version: 1.0.0
* Type: Freeware
* Supported OS: Windows 11 (64-bit)
* Author: Osakana Sankawa
* Website: https://github.com/osakana4242/scrap-renamer
* Contact: osakana4242@gmail.com

## Overview

A text editor-style file renaming tool.

After dragging and dropping files, you can edit the list of file names just like in a text editor.
Click the Execute button (or press Ctrl+S) to rename the files in bulk.

The editor uses the [Monaco Editor](https://github.com/microsoft/monaco-editor), the same editor used by [VS Code](https://github.com/microsoft/vscode).

## Main Features

- Apply edited rename changes (Ctrl+S)
- Find and Replace (Ctrl+H)
- Multiple cursors
  - Add a cursor above (Ctrl+Alt+Up)
  - Add a cursor below (Ctrl+Alt+Down)
- Case conversion (Ctrl+Shift+P)
  - UPPERCASE
  - lowercase
  - camelCase
  - snake_case
  - PascalCase

## Usage

* Run `ScrapRenamer.exe`.
* If "Windows protected your PC" is displayed, select
"More info" → "Run anyway".

## Adding to the "Send to" Menu

* Press `Win+R` to open `Run`, enter `shell:sendto` in the name field, and select `OK`.
* When the `SendTo` folder opens, drag and drop `ScrapRenamer.exe` into it while holding `Ctrl+Shift` to create a shortcut.
* Rename `ScrapRenamer.exe - Shortcut` to `ScrapRenamer`.

## License

This software is licensed under the 0BSD License.
You may use, modify, and redistribute it freely without crediting the author.
See `LICENSE.txt` for details.

## Changelog

### v1.0.0

* Initial release
