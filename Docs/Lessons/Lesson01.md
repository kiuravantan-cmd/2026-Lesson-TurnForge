# 第1回：ターン制バトルの仕組みを作る

## 今回の目標

通常攻撃でプレイヤーとCPUの番が交代し、HPが0になったら止まる仕組みを作ります。「今の状態から、行動を受け取り、次の状態を作る流れ」を自分の言葉で説明できることが目標です。

必須は通常攻撃・番の交代・勝敗・状態更新・通常攻撃を選ぶCPUまでです。回復と、HPに応じたCPUの判断は、必須部分ができた人向けの応用課題です。

## 編集するファイル

パスは `Assets/TurnForge/Scripts/` からの場所です。

| ファイル | 実装する内容 |
|---|---|
| `Battle/Rules/BattleRules.cs` | 状態のコピー、ダメージ計算、実行条件、通常攻撃。回復は応用課題 |
| `Battle/BattleModel.cs` | 行動が成功したときの状態更新 |
| `Battle/AI/AttackOnlyCommandSelector.cs` | CPUが使う技の選択 |

配布済みの `CombatantState`、`BattleState`、`BattleActionRequest`、`BattleResult` を使います。同じ名前のクラスを新しく作る必要はありません。

## 1コマ目：ターン制の流れとHP計算

### 1回の行動を処理する4つの段階

- 今の状態を持つ：HP、誰の番か、ターン番号、バトルが終わっているか。
- 行動できるか判断する：自分の番か、バトルが続いているか。
- 次の状態を計算する：相手のHPを減らし、勝敗と次の番を決める。
- 結果を表示する：採用した状態を画面へ渡す。

処理の順は「現在のBattleState → BattleActionRequest → BattleRules → BattleResult → BattleModelがNextStateを採用 → 画面へ表示」です。

Rulesは行動の結果を計算し、Modelは今の状態を覚えます。Viewは表示を担当し、Presenterが操作と表示をつなぎます。この役割分担をMVPと呼びます。今回は実際の攻撃処理に沿って、どこが何をするかを押さえましょう。

相手のHPが残る場合は次の番へ進み、HPが0になった場合は終了します。画面のHPの文字だけを変えても、戦闘の状態は変わりません。


### CopyCombatant（LESSON01-01A）

元のキャラクターの状態と、変更するHP・エネルギー・防御状態を受け取り、新しい `CombatantState` を返します。

- `Side`、`MaxHp`、`MaxEnergy` は元の状態から引き継ぐ。
- `Hp`、`Energy`、`IsGuarding` は引数の値を使う。
- 元の状態を直接書き換えない。

`get / init` のプロパティは、新しい状態を作るときに値を設定します。攻撃前の状態を残すことで、変更前と変更後を比べられます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 指定された値で、キャラクターの新しい状態を作る
/// </summary>
private static CombatantState CopyCombatant(
    CombatantState source, int hp, int energy, bool isGuarding)
{
    return new CombatantState(
        source.Side, hp, source.MaxHp,
        energy, source.MaxEnergy, isGuarding);
}
```

### ApplyDamage（LESSON01-01B）

攻撃を受けるキャラクターとダメージを受け取り、攻撃後の状態を作ります。

- 元のHPから `damage` を引く。
- HPが0未満にならないようにする。
- `CopyCombatant` に計算後のHPを渡す。
- エネルギーと防御状態は引き継ぐ。

ヒント：`Math.Max(0, 計算後のHP)` で、HPの下限を0にできます。ダメージはマスタデータから渡される値を使います。固定値の20を処理に書き込みません。防御による軽減は後の授業で追加します。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 通常攻撃後のHPを0以上にし、新しい状態を返す
/// </summary>
private static CombatantState ApplyDamage(
    CombatantState target, int damage, int guardDamageDivisor)
{
    // ダメージを受けた後のHP。マスタ側でdamageは0以上であることを確認済み。
    int nextHp = Math.Max(0, target.Hp - damage);
    return CopyCombatant(
        target, nextHp, target.Energy, target.IsGuarding);
    // 第6回でguardDamageDivisorを使った防御の軽減を追加する。
}
```

## 2コマ目：行動の条件・番の交代・勝敗を作る

### private版CanExecute（LESSON01-02）

HPを計算する前に、行動を受け付けてよいか判断します。`out BattleCommandDataRecord` を持つメソッドを編集します。

- マスタが読み込まれているか。
- 状態と指示があり、バトルが終了していないか。
- FirstとSecondの状態がそろい、HPなどが範囲内か。
- 指示を出した側が、今行動する側と一致するか。
- 指示のターン番号が、現在のターン番号と一致するか。
- 指示された技のマスタがあるか。
- この段階では通常攻撃が選ばれているか。

条件を満たさない場合は `false` を返します。条件を満たした場合は、取得した技のマスタを `out` 引数で渡します。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 第1回の通常攻撃を実行できるか確認する
/// </summary>
private bool CanExecute(
    BattleState state,
    BattleActionRequest request,
    out BattleCommandDataRecord commandData)
{
    commandData = null;

    if (!IsConfigured || state == null || request == null)
    {
        return false;
    }

    if (state.IsFinished || state.TurnNumber < 1)
    {
        return false;
    }

    if (!IsValidCombatant(state.FirstCombatant, BattleSide.First)
        || !IsValidCombatant(state.SecondCombatant, BattleSide.Second))
    {
        return false;
    }

    if (state.ActionSide != BattleSide.First
        && state.ActionSide != BattleSide.Second)
    {
        return false;
    }

    if (request.Actor != state.ActionSide
        || request.TurnNumber != state.TurnNumber)
    {
        return false;
    }

    // 必須部分は通常攻撃のみ。回復は応用課題で追加する。
    if (request.Command != BattleCommand.Attack)
    {
        return false;
    }

    return _commandData.TryGetValue(request.Command, out commandData);
}
```


### BattleRules.TryExecute（LESSON01-03）

次の順に、通常攻撃の結果を作ります。

- `result` を `null` にし、`CanExecute` で実行条件と技のマスタを取得する。
- 指示を出した側と相手の状態を取得する。
- マスタのダメージを `ApplyDamage` へ渡し、相手の攻撃後の状態を作る。
- 相手のHPが0なら終了にする。
- 続く場合は相手の番へ切り替え、ターン番号を1増やす。
- 終了する場合は、行動した側とターン番号を維持する。
- FirstとSecondの位置を保って、新しい `BattleState` を作る。
- 指示・変更前・変更後を `BattleResult` にまとめ、成功を返す。

CPUが攻撃するときもFirstはプレイヤー、SecondはCPUです。「行動した側」を毎回Firstへ入れると、キャラクターが入れ替わってしまいます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 通常攻撃を計算し、変更前後の戦闘状態を返す
/// </summary>
public bool TryExecute(
    BattleState currentState,
    BattleActionRequest request,
    out BattleResult result)
{
    result = null;

    if (!CanExecute(currentState, request, out var commandData))
    {
        return false;
    }

    // 攻撃するキャラクターと、攻撃を受けるキャラクター。
    CombatantState actor = GetCombatant(currentState, request.Actor);
    CombatantState target = GetCombatant(
        currentState, GetOpponentSide(request.Actor));

    // 第1回は防御による軽減を行わない。
    CombatantState nextTarget = ApplyDamage(
        target, commandData.Damage, 1);

    // 相手のHPが0になったか。
    bool isFinished = nextTarget.IsDefeated;
    // 勝敗が決まったときは現在の行動する番とターン番号をそのままにする。
    BattleSide nextActionSide = currentState.ActionSide;
    int nextTurnNumber = currentState.TurnNumber;

    if (!isFinished)
    {
        // ターン番号が表現できない指示は実行しない。
        if (nextTurnNumber == int.MaxValue)
        {
            return false;
        }

        nextActionSide = target.Side;
        nextTurnNumber++;
    }

    // 攻撃側にかかわらず、キャラクターをFirst、Secondの順に並べる。
    CombatantState nextFirst =
        request.Actor == BattleSide.First ? actor : nextTarget;
    CombatantState nextSecond =
        request.Actor == BattleSide.Second ? actor : nextTarget;
    // 次の時点の戦闘状態。
    BattleState nextState = new BattleState(
        nextFirst, nextSecond, nextActionSide,
        nextTurnNumber, isFinished);

    result = new BattleResult(request, currentState, nextState);
    return true;
}
```

### BattleModel.TryExecute（LESSON01-04）

- 現在の状態と指示を `BattleRules.TryExecute` へ渡す。
- 失敗した場合は現在の状態を変えず、`false` を返す。
- 成功した場合は `NextState` を現在の状態として持つ。
- 状態を置き換えた後、配布済みのイベントで結果を知らせる。

先に状態を更新することで、知らせを受け取った処理が新しいHPを読めます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 成立した行動の結果を採用し、更新後に通知する
/// </summary>
public bool TryExecute(BattleActionRequest request, out BattleResult result)
{
    result = null;

    if (_rules == null || _currentState == null)
    {
        return false;
    }

    if (!_rules.TryExecute(_currentState, request, out result))
    {
        return false;
    }

    // 通知先が最新の状態を読めるよう、先に置き換える。
    _currentState = result.NextState;
    ActionResolved?.Invoke(result);
    return true;
}
```

### AttackOnlyCommandSelector.TrySelectCommand（LESSON01-05）

- 状態があり、バトルが続いているかを調べる。
- 指定された側がFirstまたはSecondであり、その側の番かを調べる。
- 条件を満たした場合は `BattleCommand.Attack` を選び、成功を返す。

このメソッドは技を選ぶところまでです。HPの計算やターン交代は `BattleRules` に任せます。

#### ライブコーディング

対象のメソッド全体を、次のコードへ置き換えます。

```csharp
/// <summary>
/// 継続中の自分の番なら、通常攻撃を選ぶ
/// </summary>
public bool TrySelectCommand(
    BattleState state, BattleSide actor, out BattleCommand command)
{
    command = default;

    if (state == null || state.IsFinished)
    {
        return false;
    }

    if (actor != BattleSide.First && actor != BattleSide.Second)
    {
        return false;
    }

    if (state.ActionSide != actor)
    {
        return false;
    }

    command = BattleCommand.Attack;
    return true;
}
```

## 3コマ目の課題：通常攻撃の仕組みを完成させる【必須】

前半で実装した処理のうち、まだ途中になっている部分を完成させます。ここでは第1回の6か所のTODOをつなぎ、同じ戦闘本体で通常攻撃が進むようにします。

- [ ] CopyCombatantで、新しい状態を作る。
- [ ] ApplyDamageで、相手のHPを減らし、0未満にならないようにする。
- [ ] CanExecuteで、自分の番・現在のターン番号・通常攻撃の指示を受け付ける。
- [ ] TryExecuteで、攻撃後の状態を作り、続く場合は番を交代する。
- [ ] 相手のHPが0になったら、終了状態を作り、次の行動を受け付けないようにする。
- [ ] Modelが成功時だけNextStateを採用し、更新後に結果を知らせる。
- [ ] CPUが自分の番に通常攻撃を選び、同じRulesで処理されるようにする。

### ヒント

- 未完成の箇所はLESSON01-01A〜05のTODO番号から探せます。
- CPUが行動しても、FirstとSecondの配置は変えません。
- 新しい状態を作る処理と、Modelがその状態を採用する処理を分けて考えましょう。

### 応用課題A：回復処理を作る

必須部分ができた人は、次の課題へ進みます。回復は第1回の必須到達点には含めません。


BattleRules に、自分のHPを回復して相手の番へ進む処理を追加します。回復量には、講師が用意するマスタの値を使います。

- [ ] 回復するキャラクターの状態と回復量を受け取るメソッドを作る。
- [ ] 現在のHPに回復量を足す。
- [ ] 最大HPを超えないようにする。
- [ ] CopyCombatant を使い、回復後の状態を作る。
- [ ] HP以外の値を、回復前の状態から引き継ぐ。
- [ ] CanExecute で回復の指示も受け付けるようにする。
- [ ] TryExecute で通常攻撃と回復を分け、自分の回復後の状態を戦闘全体へ組み込む。
- [ ] 回復後は相手の番へ切り替え、ターン番号を1増やす。

### ヒント

- 攻撃は「相手」のHPを減らし、回復は「自分」のHPを増やします。
- Math.Min(最大HP, 計算後のHP) で、HPの上限を決められます。
- 技ごとにHPの計算を分け、計算後のターン交代と結果作成は共通にすると、同じコードを何度も書かずに済みます。
- 回復量はマスタから受け取り、処理に固定値を書き込みません。


## 4コマ目の課題：処理のつながりを説明する【必須】

通常攻撃の実装を完成させ、次の内容を自分の言葉で説明します。まだ未完成の部分がある場合は、その実装を優先してください。

- [ ] 今のHP・番・ターン番号を、どのクラスが覚えているか説明する。
- [ ] 自分の番ではない指示を、どこで受け付けないようにしているか説明する。
- [ ] HPの計算から、番の交代または勝敗が決まるまでの処理を説明する。
- [ ] 次の状態を作る処理と、現在状態を更新する処理の違いを説明する。
- [ ] プレイヤーとCPUが、同じ戦闘ルールを使う理由を説明する。

### ヒント

- BattleState、BattleActionRequest、BattleRules、BattleResult、BattleModelの順にコードをたどりましょう。
- 「どの値を読み、何を決め、どこへ渡すか」で説明すると、処理の役割がつかみやすくなります。
- 画面に表示する部分まで含めて、1回の行動の流れをつなげて考えましょう。

### 応用課題B：CPUが攻撃と回復を選ぶ

必須部分と応用課題Aの回復ができた人向けです。


AttackOnlyCommandSelector.TrySelectCommand を変更し、CPU自身のHPに応じて技を選びます。

- [ ] actor がFirstかSecondかで、行動するキャラクターの状態を選ぶ。
- [ ] そのキャラクターのHPと最大HPを読む。
- [ ] HPが最大HPの半分以下なら回復を選ぶ。
- [ ] 半分より多ければ通常攻撃を選ぶ。
- [ ] バトル終了後や、自分の番ではないときは技を選ばない。
- [ ] 選んだ技を既存の戦闘処理へ渡し、HP計算は BattleRules に任せる。

### ヒント

- 「半分以下」には、ちょうど半分も含みます。
- 最大HPが奇数のとき、整数の割り算でどの値になるか考えましょう。
- 第2コマで作った通常攻撃の選択を、HPによる分岐へ広げます。
- クラス名の変更は必須ではありません。変更する場合は、GameCompositionRoot などの参照も合わせます。



