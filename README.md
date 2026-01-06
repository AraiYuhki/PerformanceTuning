# PerformanceTuning

Unityにおけるパフォーマンス最適化の実装例を示すプロジェクトです。
3つの異なる最適化レベル（Level0、Level1、Level2）を通じて、段階的な最適化手法を学ぶことができます。

## 目次

- [開発環境](#開発環境)
- [プロジェクト概要](#プロジェクト概要)
- [実行方法](#実行方法)
- [各レベルの説明](#各レベルの説明)
- [パフォーマンス最適化の手法](#パフォーマンス最適化の手法)

## 開発環境

### 必須要件

- **Unity エディターバージョン**: 6000.3.2f1 (6000.0.30f1以降推奨)
- **対応プラットフォーム**: Windows, macOS, Linux
- **Git**: バージョン管理とプロジェクトのクローン用

### 使用パッケージ

以下のUnityパッケージが使用されています：

- **Unity Input System** (1.17.0): プレイヤー入力制御
- **TextMeshPro**: UIテキスト表示
- **Unity Jobs System**: マルチスレッド処理による最適化
- **Unity Burst Compiler**: ネイティブコード生成による高速化
- **Unity Collections**: ネイティブコンテナによるメモリ管理最適化

### 推奨スペック

最大100万個のオブジェクトを扱うため、以下のスペックを推奨します：

- **CPU**: マルチコアプロセッサ（4コア以上推奨）
- **RAM**: 8GB以上
- **GPU**: DirectX 11または12対応のグラフィックカード

## プロジェクト概要

このプロジェクトは、大量のオブジェクト（最大100万個）を効率的に処理するための3段階の最適化アプローチを実装しています：

1. **Level0**: 基本的な実装（GameObject + Object Pooling）
2. **Level1**: Jobs Systemを使用した最適化
3. **Level2**: GPU Instancingを使用した最高レベルの最適化

プレイヤーは画面内を移動でき、アイテムはプレイヤーに引き寄せられるインタラクティブな動作を実装しています。

## 実行方法

### 1. リポジトリのクローン

```bash
git clone https://github.com/ccn-arai-yuki/PerformanceTuning.git
cd PerformanceTuning
```

### 2. Unity Editorでプロジェクトを開く

1. Unity Hubを起動
2. 「プロジェクトを追加」をクリック
3. クローンしたプロジェクトフォルダを選択
4. Unity 6000.3.2f1で開く（インストールされていない場合は、Unity Hubから該当バージョンをインストール）

### 3. シーンの実行

プロジェクトには3つのシーンが含まれています：

#### Level0シーンの実行

```
Assets/Scenes/Level0.unity
```

1. Project ウィンドウで `Assets/Scenes/Level0.unity` をダブルクリック
2. Unity Editor上部の ▶️ ボタンを押して実行
3. WASDキーまたは矢印キーでプレイヤーを移動

#### Level1シーンの実行

```
Assets/Scenes/Level1.unity
```

1. Project ウィンドウで `Assets/Scenes/Level1.unity` をダブルクリック
2. Unity Editor上部の ▶️ ボタンを押して実行
3. WASDキーまたは矢印キーでプレイヤーを移動

#### Level2シーンの実行

```
Assets/Scenes/Level2.unity
```

1. Project ウィンドウで `Assets/Scenes/Level2.unity` をダブルクリック
2. Unity Editor上部の ▶️ ボタンを押して実行
3. WASDキーまたは矢印キーでプレイヤーを移動

### 4. ビルド方法

#### スタンドアロンビルド

1. `File` → `Build Settings` を選択
2. 実行したいシーン（Level0、Level1、またはLevel2）を `Scenes In Build` に追加
3. `Platform` を選択（Windows, Mac, Linux）
4. `Build` または `Build And Run` をクリック
5. ビルド先のフォルダを指定

## 各レベルの説明

### Level0: 基本実装

**場所**: `Assets/Scripts/Level0/`

**特徴**:
- GameObjectベースの実装
- Object Poolingパターンを使用してインスタンス生成コストを削減
- 個別のGameObjectとして各アイテムを管理
- Update関数で全アイテムを更新

**パフォーマンス**: ベースライン（最も低速）

### Level1: Unity Jobs System最適化

**場所**: `Assets/Scripts/Level1/`

**特徴**:
- Unity Jobs Systemを使用したマルチスレッド処理
- TransformAccessArrayでTransformへの並列アクセスを実現
- NativeArrayによる効率的なメモリ管理
- Burst Compilerによるコード最適化
- スプライトアトラスによるアニメーション最適化

**パフォーマンス**: Level0の数倍高速

**主要コンポーネント**:
- `ItemUpdateJob`: アイテムの位置計算とアニメーションをJobで並列処理
- `ItemData`: Burstコンパイル可能な構造体でアイテムデータを管理

### Level2: GPU Instancing最適化

**場所**: `Assets/Scripts/Level2/`

**特徴**:
- Graphics.DrawMeshInstancedによるGPU Instancing
- GameObjectを使用せず、完全にデータ駆動型の実装
- Unity Jobs Systemと組み合わせた最適化
- バッチ処理（1023個ずつ）によるドローコール削減
- マテリアルプロパティブロックによる効率的な描画

**パフォーマンス**: 最高速（Level0の10倍以上）

**主要コンポーネント**:
- `ItemUpdateJob`: float4x4行列をJobで並列計算
- 描画はGameObjectではなくマトリクス配列で管理

## パフォーマンス最適化の手法

このプロジェクトで実装されている主な最適化手法：

### 1. Object Pooling
- オブジェクトの再利用によりGC（ガベージコレクション）を削減
- インスタンス生成・破棄コストの削減

### 2. Unity Jobs System
- CPUのマルチコアを活用した並列処理
- メインスレッドの負荷軽減

### 3. Burst Compiler
- ネイティブコード生成による高速化
- SIMD命令の自動活用

### 4. GPU Instancing
- 同一メッシュの大量描画を1回のドローコールで実現
- GPU側での効率的な処理

### 5. NativeContainers
- マネージドヒープを使用しないメモリ管理
- GC圧力の削減

### 6. スプライトアトラス
- テクスチャの結合によりドローコール削減
- UV座標の動的変更によるアニメーション

## 操作方法

- **移動**: WASDキー または 矢印キー
- **終了**: Unity Editorの停止ボタン、またはEscキー（ビルド版）

## パフォーマンス測定

各シーンの左上に以下の情報が表示されます：

- **アクティブアイテム数 / 最大容量**: 現在表示されているアイテムの数

Unity Profilerを使用することで、より詳細なパフォーマンス情報を確認できます：

1. `Window` → `Analysis` → `Profiler` を開く
2. シーンを実行
3. CPU使用率、レンダリング時間、メモリ使用量などを確認

## ライセンス

このプロジェクトは学習目的のサンプルコードです。

## 貢献

イシューやプルリクエストを歓迎します。

## 参考リンク

- [Unity Jobs System](https://docs.unity3d.com/Manual/JobSystem.html)
- [Burst Compiler](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [GPU Instancing](https://docs.unity3d.com/Manual/GPUInstancing.html)
