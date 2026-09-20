# ScrapRenamer

## 基本情報

* ソフト名: ScrapRenamer
* バージョン: 1.0.0
* 種別: フリーウェア
* 対応OS: Windows 11 (64bit)
* 作者: 三川おさかな
* ウェブページ: https://github.com/osakana4242/scrap-renamer
* 連絡先: osakana4242@gmail.com

## 概要

テキストエディター型のファイルリネームツールです。

ファイルをドラッグ＆ドロップ後、ファイル名一覧をテキストエディター感覚で編集できます。
実行ボタン(or Ctrl+S)で一括リネームを実行します。

エディター部分は [VS Code](https://github.com/microsoft/vscode) と同じ [Monaco Editor](https://github.com/microsoft/monaco-editor) を採用しています。

## 主な機能

- 編集したリネーム内容の適用 (Ctrl+S)
- 検索・置換 (Ctrl+H)
- 複数カーソル編集
  - カーソルを上に追加 (Ctrl+Alt+Up)
  - カーソルを下に追加 (Ctrl+Alt+Down)
- 文字種の変換 (Ctrl+Shift+P)
  - 大文字 (UPPERCASE)
  - 小文字 (lowercase)
  - キャメルケース (camelCase)
  - スネークケース (snake_case)
  - パスカルケース (PascalCase)


## 使用方法

* `ScrapRenamer.exe` を実行します。
* 「Windows によって PC が保護されました」と表示された場合は、
「詳細情報」→「実行」を選択します。

## 「送る」メニューへの追加方法

* `Win+R` で `ファイル名を指定して実行` を開き、名前欄に `shell:sendto` と入力し `OK` を選択します。
* `SendTo` フォルダが開くので、ここに `ScrapRenamer.exe` を `Ctrl+Shift` を押しながらドラッグ＆ドロップして、ショートカットを作成します。
* `ScrapRenamer.exe - ショートカット` を `ScrapRenamer` にリネームします。

## ライセンス

このソフトウェアは 0BSD ライセンスです。
作者へのクレジット表記なしで、自由に使用・改変・再配布できます。
詳しくは `LICENSE.txt` を参照してください。

## 更新履歴

### v1.0.0

* 初回公開
