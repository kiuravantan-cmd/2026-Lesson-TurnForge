# Unityのデバッグ入門
## ログとVisual Studio 2022で原因を探す

対象：Windows・Unity 6・Visual Studio 2022。講義の本編とは別に、困ったときに開く補助資料です。メニュー名は表示言語や更新によって少し変わります。Visual Studio Codeとは操作が異なります。

この資料でできるようになること：

* Consoleへ処理の通過点と変数の値を出す。
* 通常ログ・警告・エラーを使い分ける。
* Unityへデバッガーを接続し、ブレークポイントで止める。
* 変数と呼び出し元を見て、予想と違った場所を探す。

## 1．デバッグは「どこから予想と違ったか」を探すこと

「攻撃したのにHP表示が変わらない」なら、入力が届いていないのか、HPの計算が違うのか、表示へ渡していないのかを分けます。最初から全部を書き換えず、1か所ずつ値と処理の流れを追います。

* ログ：動かしたまま、通った場所や値の記録をConsoleへ残す。前後の順序を比べたいときに使う。
* ブレークポイント：指定したコードへ来たところで実行を止め、その時点の変数を見る。関数の中へ進んだり、呼び出し元へ戻って調べたりできる。

まずログで場所を絞り、詳しい値はブレークポイントで見る、という使い方もできます。

## 2．UnityのConsoleを開く

1. UnityのWindow → General → Consoleを開く。
2. 通常ログ・警告・エラーの表示ボタンをすべてオンにし、検索欄を空にする。
3. 順番や回数を調べるときはCollapseをオフにする。同じ内容がまとめられると、出た回数を読み違えやすくなる。
4. 必要ならClearで古い表示を消してから、調べたい操作を1回だけ行う。
5. メッセージを選んで詳細と呼び出し元を見る。自分のスクリプトの行をダブルクリックすると、そのコードを開ける。

Clear on Playがオンなら、Playを始めると前の表示が消えます。保存前後など、Playをまたいで比べたい場合はオフにします。ClearはConsoleの表示を消す操作で、保存ファイルを消す操作ではありません。

Error Pauseは、Debug.LogErrorなどのエラーでUnityのPlayを一時停止させる設定です。Visual Studioのブレークポイントで止める操作とは別です。通常のDebug.LogではError Pauseによる停止はしません。

参考：Unity公式・Consoleの各ボタン。
https://docs.unity3d.com/6000.0/Documentation/Manual/Console.html

## 3．ログの種類と書き方

### Debug.Log：通った場所と値を見る

```csharp
Debug.Log("[攻撃] 決定された");
Debug.Log($"[HP計算] 現在HP={hp} ダメージ={damage}");
```

1行目は処理が来たこと、2行目はそのときの値を残します。DebugはUnityEngineにあるクラスです。ファイル先頭のusing UnityEngine;で短く書けます。

$"..."は、文字列の{ }へ変数や式の値を入れる書き方です。「hp」という文字だけでなく、その時点の100などの値が出ます。hpとdamageは説明用の変数名なので、自分の関数で使える実際の変数名へ合わせます。

### Debug.LogWarning：注意が必要な状態を知らせる

```csharp
Debug.LogWarning("[保存] 今回は保存できなかった。未保存の変更を残す");
```

黄色い警告として出ます。例は、ゲーム全体を止めずに、次の機会に保存を試す場合です。普段の処理をすべて警告にすると、本当に見たい警告を探しにくくなります。

### Debug.LogError：処理を続けられない問題を知らせる

次はMonoBehaviourのvoid関数内で、参照がない場合に戻る例です。

```csharp
if (_target == null)
{
    Debug.LogError("[表示] 対象が割り当てられていない", this);
    return;
}
```

赤いエラーとして出ます。_targetは説明用の参照名です。重要なのは、LogError自体がその関数からreturnするわけではないことです。ここでは明示的なreturnで、その関数の残りを実行しないようにしています。

Consoleのエラーには、自分のLogErrorだけでなく、コンパイルエラーや例外の報告もあります。メッセージとコードの場所を読み、同じ原因だと思い込まないようにします。

### Debug.LogException：捕まえた例外の詳細を出す

既にあるcatchの中でexceptionを受け取っている場合は、次の形で例外と呼び出し情報を出せます。

```csharp
Debug.LogException(exception);
```

exceptionはcatchで受け取る例外オブジェクトです。LogExceptionはそれを表示する関数で、throwしているわけではありません。ログを出すためだけに、すべての処理をtry／catchで囲む必要はありません。

### 関連するGameObjectも渡す

MonoBehaviourの関数内では、次のように第2引数へthisを渡せます。

```csharp
Debug.Log($"[初期化] オブジェクト={name}", this);
```

Consoleでそのログを選ぶと、関連するオブジェクトを探しやすくなります。thisは「このコンポーネント」です。BattleSessionServiceやBattlePresenterのような普通のC#クラスはUnityEngine.Objectではないので、同じように第2引数へthisを渡しません。そこではDebug.Log(message)を使います。

Debugの名前がほかのライブラリとぶつかる場合や、using UnityEngine;がないファイルでは、UnityEngine.Debug.Log(...)と省略せずに書けます。

公式リファレンス：各関数の引数と動作。
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Debug.Log.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Debug.LogWarning.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Debug.LogError.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Debug.LogException.html

## 4．ログを入れる場所を決める

通過点に名前を付け、「呼び出す直前」「戻ってきた直後」「途中でreturnする条件」を見ると、止まった場所を絞れます。

次はTurnForgeのBattlePresenter.HandleStateChangedで使う例です。既存のnull・終了済み判定を通った後、_view.Render(state);の直前へ一時追加します。新しい関数を作るコードではありません。

```csharp
UnityEngine.Debug.Log(
    $"[状態通知] ターン={state.TurnNumber} " +
    $"自分HP={state.FirstCombatant.Hp} " +
    $"相手HP={state.SecondCombatant.Hp}");
```

このログが正しいのに画面が違うなら、次はRenderと表示先の参照を見ます。ログ自体が出ないなら、その通知へ来るまでを見ます。

### 結果を見るために同じ処理を2回呼ばない

TrySaveの結果を知りたいとき、ログの中でTrySaveをもう一度呼ぶと、調べるためのコードがゲームの動きを変えてしまいます。元の呼び出しを一時変数へ置き換え、その結果をログとifの両方で使います。以下は既存のif (!TrySave())の代わりに使う形で、元の呼び出しは残しません。

```csharp
bool saved = _battleSessionService.TrySave();
Debug.Log($"[保存結果] 成功={saved}");

if (!saved)
{
    Debug.LogWarning("保存できなかった");
}
```

### ログを増やしすぎない

* UpdateやFixedUpdateへ無条件に入れると大量に出る。まず必要な操作が成立したときだけ出す。
* nullかもしれない相手の値を読むログで、新しい例外を起こさない。先にnull判定するか、必要な場所では?.を使う。
* 「来た」だけを何か所にも書かず、[保存入口]・[読み込み結果]のように場所を分ける。
* 調べ終わった一時ログは外す。失敗を知らせるために元からある警告・エラーは残す。

## 5．練習用の小さなコード

この例はTurnForge本体を変えず、ログとブレークポイントを試すためのものです。試す場合だけ、DebugPractice.csという新しいファイルを作り、空のGameObjectへ付けます。InspectorでHPとダメージを設定してからPlayします。

```csharp
using UnityEngine;

/// <summary>ログとブレークポイントでHP計算を調べる練習。</summary>
public sealed class DebugPractice : MonoBehaviour
{
    /// <summary>計算前のHP。</summary>
    [SerializeField] private int _hp = 100;

    /// <summary>今回受けるダメージ。</summary>
    [SerializeField] private int _damage = 20;

    /// <summary>1回だけ計算し、前後の値を表示する。</summary>
    private void Start()
    {
        if (_damage < 0)
        {
            Debug.LogError("ダメージを0以上にしてください", this);
            return;
        }

        Debug.Log($"[計算前] HP={_hp} ダメージ={_damage}", this);

        int nextHp = Mathf.Max(0, _hp - _damage);
        _hp = nextHp;

        Debug.Log($"[計算後] HP={_hp}", this);

        if (_hp > 0 && _hp <= 25)
        {
            Debug.LogWarning("HPが少なくなっています", this);
        }
    }
}
```

* HP100・ダメージ20：計算前100、計算後80のログが出る。
* HP30・ダメージ20：計算後10になり、警告も出る。
* ダメージ-1：エラーを出してreturnするので、HP計算へ進まない。

この練習の警告は固定値25です。TurnForge本体の「最大HPの4分の1」とは別の、最小の説明用コードです。既に付けたコンポーネントの値はInspectorで見ます。コードの初期値を書き換えても、Inspectorに保存済みの値がそのまま使われる場合があります。

## 6．Visual Studio 2022をUnityへ接続する準備

1. Visual Studio InstallerでVisual Studio 2022の「変更」を開き、「Unityによるゲーム開発」のワークロードを入れる。Visual Studio Tools for Unityが使える状態にする。
2. UnityのPackage ManagerでVisual Studio Editorパッケージがあることを見る。Visual Studio Code用のパッケージと取り違えない。
3. UnityのEdit → Preferences → External Toolsで、External Script EditorにVisual Studio 2022を選ぶ。
4. UnityのProjectウィンドウからC#スクリプトをダブルクリックする。UnityのプロジェクトをVisual Studioで開く。スクリプト1枚だけを単独で開いた状態にはしない。
5. Unityのコンパイルエラーを解消する。
6. Unity右下のCode OptimizationがDebug Modeかを見る。Release Modeなら切り替える。接続時にDebug Modeへ変更する案内が出た場合は、その内容に従う。

ここでのDebug ModeはUnity EditorのC#実行の設定です。Build ProfilesのDevelopment BuildやScript Debuggingは、実行ファイルを調べる場合の設定で、今回のEditor内の練習には不要です。

準備の公式手順：
https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity?pivots=windows
https://docs.unity3d.com/6000.0/Documentation/Manual/managed-code-debugging.html

## 7．ブレークポイントで止める

### 練習コードで止める手順

1. Playを止めた状態で、Visual StudioのDebugPractice.csを開く。
2. int nextHp = Mathf.Max(0, _hp - _damage);の行の左端の余白をクリックする。赤い丸が付く。行へカーソルを置いてF9でも切り替えられる。
3. Visual Studio上部の「Unityにアタッチ（Attach to Unity）」を押す。見つからない場合は「デバッグ → Unityデバッガーのアタッチ（Attach Unity Debugger）」から、対象プロジェクトのEditorを選ぶ。
4. Unityへ戻り、Playを押す。
5. Startがその行へ来ると、Visual Studioで黄色い実行位置が表示され、Unityの実行が止まる。

通常のAttach to Unityは接続する操作です。UnityのPlayも別に押します。「Attach to Unity and Play」を選んでいる場合は、接続とPlayを一緒に始められます。

Startは開始時に1回なので、Startが終わってから接続しても、その回のStartへは戻れません。停止して、接続したままもう一度Playします。攻撃処理などの関数で止めたい場合は、接続後にその操作を行います。

### 黄色い行は、基本的に「これから実行する行」

計算の行で止まった時点では、その行の代入はまだ終わっていません。_hpが100・_damageが20なら、F10で計算を進めた後にnextHpが80になり、さらに_hp = nextHp;を進めた後に_hpが80になります。

ブレークポイントで止まっている間、Unityが操作へ応答しないことがあります。フリーズだと決め付けず、Visual Studioの黄色い行を見ます。再開するときはF5です。UnityのPauseボタンで止めることと、C#の途中で止めることは別です。

接続とPlayの公式手順：
https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/using-visual-studio-tools-for-unity?pivots=windows

## 8．止まった後の基本操作

ショートカットは標準的な設定です。キーボードや設定が違う場合は、Visual Studioの「デバッグ」メニューから選びます。

* F9：ブレークポイントの設定・解除。
* F5／続行：実行を再開し、次のブレークポイントなどで止まるまで進む。
* F10／ステップオーバー：呼び出す関数の中を追わず、現在の文を進める。関数を飛ばして実行しないという意味ではない。
* F11／ステップイン：呼び出す関数の中へ入って追う。最初は自分の関数を調べたいときに使う。外部コードなどには入れない場合もある。
* Shift＋F11／ステップアウト：今いる関数の残りを実行して、呼び出し元へ戻る。
* Shift＋F5／デバッグの停止：デバッガーの接続を終了する。UnityのPlayが続いている場合は、Unity側でも停止する。

再開するために黄色い矢印を別の行へ動かす必要はありません。実行位置を手動で変える機能は計算や初期化を飛ばすことがあるので、最初は使わずF10とF5で進めます。

ステップ操作の公式説明：
https://learn.microsoft.com/en-us/visualstudio/debugger/navigating-through-code-with-the-debugger?view=vs-2022

## 9．変数と呼び出し元を見る

### マウスを重ねる・ローカル・ウォッチ

止まっている間に_hpなどの変数へマウスを重ねると、その時点の値を見られます。オブジェクトは展開して中の値を見ます。

「デバッグ → ウィンドウ → ローカル（Locals）」では、今の関数で使える変数を一覧で見ます。「ウォッチ（Watch）→ ウォッチ1」には、追いたい変数名や式を登録できます。

練習では_hp・_damage・nextHpを見ます。nextHpを代入する前は、まだ使える値として表示されない場合があります。「まだ代入していない」ことと「計算結果が0だった」ことを分けます。

ウォッチへTrySave()やTryExecute(...)のような状態を変える関数を書かないでください。値を見るつもりで処理を実行してしまうことがあります。まず変数・フィールドを見て、プロパティにも計算や処理が含まれ得ると覚えておきます。

変数を見る公式説明：
https://learn.microsoft.com/en-us/visualstudio/debugger/autos-and-locals-windows?view=vs-2022

### 呼び出し履歴（Call Stack）

「デバッグ → ウィンドウ → 呼び出し履歴」を開くと、今の関数がどこから呼ばれたかを追えます。呼び出し元の行を選ぶと、その関数のコードと変数を調べられます。呼び出し元を選ぶ操作で、ゲームの実行を巻き戻しているわけではありません。

TurnForgeなら、保存関数へ来たときに「自分の番の通知から来たのか」「別の場所から来たのか」を探す助けになります。awaitやイベントをまたぐ処理では、過去の流れ全部が普通の関数呼び出しとして残るとは限りません。通過点のログも一緒に使います。

呼び出し履歴の公式説明：
https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-use-the-call-stack-window?view=vs-2022

## 10．条件付きブレークポイント

毎回止まると追いにくいときに使います。練習コードの_hp = nextHp;の行へブレークポイントを置き、赤い丸を右クリックして「条件（Conditions）」を開きます。条件式にnextHp <= 25を設定すると、その条件を満たす場合だけ止まります。

nextHpを作る前の行へ置くと、まだその変数を条件に使えません。「その位置で存在する値」を条件へ書きます。条件式にも状態を変える関数は入れません。

赤い丸を右クリックして無効化すれば、位置を残して一時的に止まらなくできます。条件付きブレークポイントなどの対応範囲や表示は、Unity連携・デバッガーの版で異なる場合があります。

参考：ブレークポイントと条件の設定。
https://learn.microsoft.com/en-us/visualstudio/debugger/using-breakpoints?view=vs-2022

## 11．TurnForgeでは、どこに置くとよいか

コードの位置は行番号より、関数名と処理の内容で探します。授業の進み具合で、まだない関数は対象にしません。

* 入力が届かない：BattleView.HandleConfirmの最初と、ConfirmRequestedを呼ぶ行。途中の条件でreturnしていないか、_isInputEnabled・_selectedCommandを見る。
* 行動が受け付けられない：BattlePresenter.HandleConfirmRequestedの条件と、BattleModel.TryExecute。操作可否・現在の番・コマンドの条件を見る。
* HPが変わらない：BattleRulesのダメージや回復の計算と、BattleModelで次の状態を採用する行。計算前と計算後の状態を比べる。
* 表示が変わらない：BattlePresenter.HandleStateChangedとBattleView.Render。届いたstateのHPと、表示先の参照を見る。
* 保存されない：GameCompositionRoot.HandlePlayerTurnReady → BattleSessionService.TryCapture → TrySave。初期化済みか、保存する状態とマスタID、未保存の印を見る。
* 続きへ戻らない：BattleSessionService.TryLoadとTryRestore、RootでBattleModelへ状態を渡す行。保存データと復元したHPを比べる。

ファイル操作やオンライン通信の途中を長く止めると、相手の処理が進んだり待ち時間が切れたりします。停止したことで変わる問題には、ログを優先することもあります。

## 12．止まらない・ログが出ないとき

* ログが出ない：Consoleの検索と種類別ボタンを見て、そもそもその関数が呼ばれるかを調べる。GameObjectやコンポーネントが無効になっていないかも見る。
* 赤い丸が白抜き・警告付き：その行で止められる状態ではない。表示される理由を読み、接続先・Debug Mode・コンパイル完了を見直す。コメントや空行ではなく、実行される文へ置く。
* 接続しても止まらない：別のUnityプロジェクトへ接続していないか、その操作で本当にその行を通るかを見る。Startは接続前に終わっていないかも見る。
* Attach to Unityがない：Unityによるゲーム開発のワークロード、Visual Studio Editorパッケージ、External Script Editorを見直す。Unityからプロジェクトを開き直す。
* 古いコードで止まる・値が合わない：保存してUnityへ戻り、コンパイル完了を待つ。いったんPlayとデバッグを止め、接続し直す。
* Unityが固まったように見える：Visual Studioがブレークポイントで止まっていないかを見る。続行はF5。

## 最後に：原因を1つずつ確かめる

再現する操作を決める → 予想する値を書く → 通過点へログを入れる → 最初に違う場所で止める → 原因の箇所だけ直す → 同じ操作でもう一度試す、の順で進めます。

調べ終わったら一時ログ・ブレークポイント・練習用コンポーネントを片付けます。「止まらなくなった」だけで終わらず、攻撃・回復・保存など、本来やりたかった操作ができるかを見ます。

