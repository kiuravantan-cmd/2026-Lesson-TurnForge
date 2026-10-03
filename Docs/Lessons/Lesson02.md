# 第2回：選択・決定・取消と入力の接続

## 編集するファイル

パスは `Assets/TurnForge/Scripts/` からの場所です。

| ファイル | 実装する内容 |
|---|---|
| `UI/Battle/BattlePresenter.cs` | 入力受付、技の選択・決定・取消、使える技の表示 |
| `UI/Battle/BattleView.cs` | ボタン操作を共通のイベントへ接続 |
| 操作方式の設定用ファイル（授業前に配布） | パッド・キーボード・クリックの方式選択と切り替え |

第1回で作った `BattleRules` と `BattleModel` を使います。InputActionの作成方法は前期に扱ったため、今回は配布済みのUI入力を戦闘の指示へ接続します。

操作はどの方式でも「技を選ぶ → 決定する」です。技ボタンへのフォーカス移動と、戦闘で使う技の選択を区別します。クリックでも、技を選んだ後に決定ボタンを押します。

## 1コマ目：Presenterで選択・決定・取消を作る

### CanAcceptPlayerInput（LESSON02-01）

次の条件をすべて満たすときだけ、プレイヤーの操作を受け付けます。

- 初期化が終わっている。
- 破棄されていない。
- 行動・演出の処理中ではない。
- `BattleFlowController` が入力を受け付けている。
- 現在の状態があり、バトルが続いている。
- 現在の `ActionSide` が `_inputSide` と一致する。

#### ライブコーディング

対象のメソッドまたはプロパティ全体を、次のコードへ置き換えます。

```csharp
/// <summary>プレイヤーの操作を受け付けられるか。</summary>
private bool CanAcceptPlayerInput =>
    _isInitialized && !_isDisposed && !_isExecuting
    && _view != null && _flow != null && _flow.CanAcceptInput
    && _model?.CurrentState != null
    && !_model.CurrentState.IsFinished
    && _model.CurrentState.ActionSide == _inputSide;
```

### HandleCommandSelected（LESSON02-02）

- `CanAcceptPlayerInput` を調べる。
- `BattleModel.CanExecute(_inputSide, command)` で、その技を使えるか調べる。現在のターン番号はModel側で使われます。
- 使える場合は `_selectedCommand` に覚え、Viewの選択表示を更新する。

技を選んだだけでは、HPやターン番号を変えません。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>使える技を選び、表示へ反映する。</summary>
private void HandleCommandSelected(BattleCommand command)
{
    if (!CanAcceptPlayerInput || !_model.CanExecute(_inputSide, command))
    {
        return;
    }

    _selectedCommand = command;
    _view.SetSelectedCommand(_selectedCommand);
    RefreshInput();
}
```

### HandleConfirmRequested（LESSON02-03）

- 操作を受け付けられ、技が選ばれているか調べる。
- 現在のターン番号を使って `BattleActionRequest` を作る。
- 実行条件をもう一度調べる。
- 配布済みの `TryExecuteAsync` へ指示を渡す。

選んだ後に状態が変わる場合があるため、決定時にも条件を調べます。入力方式ごとに攻撃処理を作らず、同じ実行先へ渡します。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>選択中の技を、現在のターンの指示として送る。</summary>
private void HandleConfirmRequested()
{
    if (!CanAcceptPlayerInput || !_selectedCommand.HasValue)
    {
        return;
    }

    // 選択後に状態が変わる場合に備え、決定時にも条件を調べる。
    BattleCommand command = _selectedCommand.Value;
    if (!_model.CanExecute(_inputSide, command))
    {
        RefreshInput();
        return;
    }

    // 現在の番号を使い、配布済みの実行処理へ合流する。
    var request = new BattleActionRequest(
        _inputSide, _model.CurrentState.TurnNumber, command);
    TryExecuteAsync(request).Forget();
}
```

### HandleCancelRequested（LESSON02-04）

- 操作を受け付けられるか調べる。
- `_selectedCommand` を `null` にする。
- Viewの選択表示を消し、ボタンの状態を更新する。

取消では戦闘状態を変えません。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>技の選択を取り消す。</summary>
private void HandleCancelRequested()
{
    if (!CanAcceptPlayerInput)
    {
        return;
    }

    _selectedCommand = null;
    _view.SetSelectedCommand(null);
    RefreshInput();
}
```

### RefreshInput（LESSON02-05）

- 操作全体を受け付けられるか調べる。
- 各技について `BattleModel.CanExecute(_inputSide, command)` で使用できるか調べる。
- `SetCommandEnabled` で各技のボタンへ反映する。
- 選択中の技が使えなくなった場合は、選択を解除する。
- 選択表示と `SetInputEnabled` を更新する。

防御・チャージ・必殺技は、ルールを実装する後の授業まで使用不可にします。第1回の回復課題を実装済みなら、回復も対象に加えます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>使える技と選択状態を、画面へ反映する。</summary>
private void RefreshInput()
{
    if (_isDisposed || !_isInitialized)
    {
        return;
    }

    // 操作全体を受け付けられるか。
    bool canAccept = CanAcceptPlayerInput;
    foreach (BattleCommand command in Enum.GetValues(typeof(BattleCommand)))
    {
        // 回復を追加した場合も、同じルールで使用条件を調べる。
        bool canUse = canAccept && _model.CanExecute(_inputSide, command);
        _view.SetCommandEnabled(command, canUse);
    }

    if (_selectedCommand.HasValue
        && (!canAccept || !_model.CanExecute(_inputSide, _selectedCommand.Value)))
    {
        _selectedCommand = null;
    }

    _view.SetSelectedCommand(_selectedCommand);
    _view.SetInputEnabled(canAccept);
    // 第4回で、状態変化の通知をR3へ広げる。
}
```

## 2コマ目：Viewの共通通知へ3種類の操作をつなぐ

### NotifySelection（LESSON02-06）

入力が有効で、指定された技が `_availableCommands` に含まれる場合だけ、`CommandSelected` を通知します。技の選択を覚える処理はPresenterへ任せます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>使用可能な技の選択を通知する。</summary>
private void NotifySelection(BattleCommand command)
{
    if (!isActiveAndEnabled || !_isInputEnabled
        || !_availableCommands.Contains(command))
    {
        return;
    }

    CommandSelected?.Invoke(command);
}
```

### HandleConfirm（LESSON02-07）

入力が有効で、技が選ばれており、その技が使える場合だけ `ConfirmRequested` を通知します。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>使用可能な選択があるときに、決定を通知する。</summary>
private void HandleConfirm()
{
    if (!isActiveAndEnabled || !_isInputEnabled
        || !_selectedCommand.HasValue
        || !_availableCommands.Contains(_selectedCommand.Value))
    {
        return;
    }

    ConfirmRequested?.Invoke();
}
```

### HandleCancel（LESSON02-08）

入力が有効で、技が選ばれている場合だけ `CancelRequested` を通知します。使えなくなった技も取り消せるように、技の使用条件は取消の条件に含めません。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>選択中の技を取り消す操作を通知する。</summary>
private void HandleCancel()
{
    if (!isActiveAndEnabled || !_isInputEnabled || !_selectedCommand.HasValue)
    {
        return;
    }

    CancelRequested?.Invoke();
}
```

### OnCancel（LESSON02-09）

- イベントがあり、まだ使われていないか調べる。
- Viewが有効で、取消を受け付けられるか調べる。
- 受け付けたイベントを `Use()` で使用済みにする。
- `HandleCancel` を呼ぶ。

パッド・キーボードの取消と、画面の取消ボタンを同じ処理へつなぎます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>キー・パッドの取消を、ボタンと同じ処理へ渡す。</summary>
public void OnCancel(BaseEventData eventData)
{
    if (eventData == null || eventData.used
        || !isActiveAndEnabled || !_isInputEnabled
        || !_selectedCommand.HasValue)
    {
        return;
    }

    // 使用済みにしてから通知し、同じイベントの重複処理を防ぐ。
    eventData.Use();
    HandleCancel();
}
```

### 操作方式の設定を接続する

講師が配布する設定画面と入力切り替えの枠へ、次の処理を実装します。

- パッド・キーボード・クリックの選択を、操作方式として覚える。
- 選択された方式のUI入力を有効にし、他の方式の入力を止める。
- 方式を切り替えるとき、選択中の技を解除する。
- パッド・キーボードでは、操作可能なボタンへフォーカスを置く。
- クリックでは、ボタンのクリックを同じ選択・決定・取消の通知へ渡す。

設定の変更では、HP・行動する番・ターン番号を変えません。操作方式の保存は第3回で追加します。

## 3コマ目の課題：方式の切り替えを完成させる

- [ ] パッド・キーボード・クリックを、設定画面で選べるようにする。
- [ ] 選んだ方式を画面へ表示する。
- [ ] 選ばれている方式だけが戦闘操作を送るようにする。
- [ ] 方式を切り替えたら、選択中の技と表示を消す。
- [ ] パッド・キーボードへ切り替えたら、使用可能な技ボタンへフォーカスを置く。
- [ ] 通常攻撃のボタンを `CommandSelected` へ接続する。回復の応用課題を実装済みの場合は、回復ボタンも同じ通知へ接続する。
- [ ] どの方式でも、決定を同じ `ConfirmRequested` へ接続する。

### ヒント

- 入力方式が変わっても、Presenterが受け取るイベントは同じです。
- 技の選択解除は、Presenterが持つ値とViewの表示の両方へ反映します。
- 無効なボタンへフォーカスを置かず、現在使える技から選びます。
- 第1回の回復は応用課題です。未実装の場合は、通常攻撃で第2回の必須部分を進めます。

## 4コマ目の課題：連打・取消・パッド未接続への対応

- [ ] 技を選んでいないときは、決定と取消のボタンを無効にする。
- [ ] 取消したときは、技の選択表示を消す。
- [ ] 決定を受け付けた直後に、次の決定を受け付けないようにする。
- [ ] CPUの番・行動処理中・バトル終了後は、戦闘操作を受け付けない。
- [ ] パッドが接続されていないときは、案内を表示する。
- [ ] パッドがなくても、キーボードまたはクリックで設定画面へ戻れるようにする。
- [ ] 設定画面へ戻った後、別の方式を選び直せるようにする。

### ヒント

- `TryExecuteAsync` には、実行中のフラグと入力を止める処理が用意されています。そこへ合流させると、方式ごとの二重実行を防げます。
- Viewのボタンだけでなく、Presenterと戦闘ルールにも受付条件を置きます。
- 取消は「技の選択を消す」、設定画面への復帰は「操作方式を選び直す」と役割を分けます。
- パッド方式でも、設定画面へ戻るためのキーボード・クリック操作は残します。

