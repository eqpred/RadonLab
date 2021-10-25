using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RadonLab {

  /// <summary>
  /// 文字列で表記された式を評価します
  /// </summary>
  public static class Formula {

    /// <summary>
    /// 式を構成する要素の種類を定義します
    /// </summary>
    public enum TokenType {
      /// <summary>
      /// 論理
      /// </summary>
      Logic,
      /// <summary>
      /// 数値
      /// </summary>
      Numeric,
      /// <summary>
      /// 変数
      /// </summary>
      Variable,
      /// <summary>
      /// 演算子
      /// </summary>
      Operator,
      /// <summary>
      /// 関数
      /// </summary>
      Function,
    }

    /// <summary>
    /// 式を構成する要素を提供します
    /// </summary>
    public class Token {
      /// <summary>
      /// トークンの名前を取得または設定します
      /// </summary>
      public string Name { get; set; }
      /// <summary>
      /// トークンタイプを取得または設定します。
      /// </summary>
      public TokenType Type { get; set; }
      /// <summary>
      /// トークンの値を取得または設定します。
      /// </summary>
      public dynamic Value { get; set; }
      /// <summary>
      /// トークンが処理するフィールド数を取得または設定します。
      /// </summary>
      public int NumberOfFields { get; set; }

      //----------------------------------------------------

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Type"></param>
      /// <param name="Value"></param>
      /// <param name="Name"></param>
      /// <param name="NumberOfFields"></param>
      public Token(string Name, TokenType Type, dynamic Value, int NumberOfFields = 0) {
        this.Name = Name;
        this.Type = Type;
        this.Value = Value;
        this.NumberOfFields = NumberOfFields;
      }

      /// <summary>
      /// トークンの内容を文字列で返します。
      /// </summary>
      /// <returns>トークンを示す文字列を返します。</returns>
      public override string ToString() => $@"{(Name != "" ? $"{Name}=" : "")}{Value},{Type},{NumberOfFields}";
    }

    /// <summary>
    /// 式を構成する要素のコレクションを提供します
    /// </summary>
    public class TokenCollection : IList<Token> {
      private List<Token> Tokens = new List<Token>();

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public TokenCollection() { }
      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Tokens"></param>
      public TokenCollection(TokenCollection Tokens) {
        foreach(var Entry in Tokens)
          this.Tokens.Add(Entry);
      }
      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Tokens"></param>
      public TokenCollection(IEnumerable<Token> Tokens) {
        foreach(var Entry in Tokens)
          this.Tokens.Add(Entry);
      }

      //----------------------------------------------------

      #region コレクション処理
      public Token this[int index] { get => Tokens[index]; set => Tokens[index] = value; }
      public Token this[string name] {
        get => Tokens.Find(Token => Token.Name == name);
        set {
          var Index = Tokens.FindIndex(Token => Token.Name == name);
          if(Index != -1)
            Tokens[Index].Value = value;
        }
      }
      public int Count => Tokens.Count;
      public bool IsReadOnly => ((ICollection<Token>)Tokens).IsReadOnly;
      public void Add(Token item) => Tokens.Add(item);
      public void AddRange(List<Token> items) => items.ToList().ForEach(item => Tokens.Add(item));
      public void AddRange(TokenCollection items) => items.ToList().ForEach(item => Tokens.Add(item));
      public void Clear() => Tokens.Clear();
      public bool Contains(Token item) => Tokens.Contains(item);
      public void CopyTo(Token[] array, int arrayIndex) => Tokens.CopyTo(array, arrayIndex);
      public int IndexOf(Token item) => Tokens.IndexOf(item);
      public void Insert(int index, Token item) => Tokens.Insert(index, item);
      public bool Remove(Token item) => Tokens.Remove(item);
      public void RemoveAt(int index) => Tokens.RemoveAt(index);
      public IEnumerator<Token> GetEnumerator() => ((IEnumerable<Token>)Tokens).GetEnumerator();
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)Tokens).GetEnumerator();
      #endregion
    }

    /// <summary>
    /// 変数を提供します
    /// </summary>
    public class Variable {
      /// <summary>
      /// 詳細モードを取得または設定します
      /// </summary>
      public bool IsVerbose { get; set; } = false;
      /// <summary>
      ///  パラメータ名を取得または設定します
      /// </summary>
      public string Name { get; set; } = "";
      /// <summary>
      /// パラメータの最新値値を取得または設定します
      /// </summary>
      protected dynamic LatestValue { get; set; } = false;
      /// <summary>
      /// トークンコレクション内のパラメータ位置を取得します
      /// </summary>
      public List<int> Indices { get; set; } = new List<int>();
      /// <summary>
      /// 値の更新を通知します
      /// </summary>
      public event EventHandler<DateTime> OnUpdated;
      /// <summary>
      /// 値の変化を通知します
      /// </summary>
      public event EventHandler<DateTime> OnChanged;
      /// <summary>
      /// 比較値の変更を排他制御します
      /// </summary>
      private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public Variable() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Name"></param>
      /// <param name="IsVerbose"></param>
      public Variable(string Name, bool IsVerbose = false) { this.Name = Name; this.IsVerbose = IsVerbose; }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Name"></param>
      /// <param name="Value"></param>
      /// <param name="IsVerbose"></param>
      public Variable(string Name, dynamic Value, bool IsVerbose = false) { this.Name = Name; this.LatestValue = Value; this.IsVerbose = IsVerbose; }

      //----------------------------------------------------

      /// <summary>
      /// 変数の値を取得または設定します
      /// </summary>
      public dynamic Value {
        get => LatestValue;
        set {
          DateTime Stamp = DateTime.Now;
          //Semaphore.Wait();
          var OldValue = LatestValue;
          LatestValue = Convert.ChangeType(value, LatestValue.GetType());//型に合わせて最新値を更新
          //Semaphore.Release();
          if(OldValue.CompareTo(LatestValue) != 0) {//前回値と異なるなら
            if(IsVerbose)
              Console.WriteLine($"{Stamp:HH:mm:ss.ff} | '{Name}' changed {OldValue} -> {LatestValue}");
            OnChanged?.Invoke(this, Stamp);//変更を通知
          } else //前回値と同じなら
            OnUpdated?.Invoke(this, Stamp);//更新を通知
        }
      }

      /// <summary>
      /// 変数の値を設定します。イベントは発生しません
      /// </summary>
      /// <param name="NewValue"></param>
      public void SetValue(dynamic value) {
        //Semaphore.Wait();
        LatestValue = value != null ? value : LatestValue;//最新値を更新
        //if(IsVerbose)
        //  Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | '{Name}' renewed {LatestValue}");
        //Semaphore.Release();
      }

      /// <summary>
      /// 現在のオブジェクトを表す文字列を返します
      /// </summary>
      /// <returns></returns>
      public override string ToString() => $"'{Name}'={LatestValue}";
    }

    /// <summary>
    /// 変数のコレクションを提供します
    /// </summary>
    public class VariableCollection : IList<Variable> {
      /// <summary>
      /// パラメータ情報を保持します
      /// </summary>
      private List<Variable> Variables = new List<Variable>();

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public VariableCollection() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Variables"></param>
      public VariableCollection(IEnumerable<Variable> Variables) => this.Variables.AddRange(Variables);

      //----------------------------------------------------

      #region コレクション処理
      public Variable this[int index] {
        get => Variables[index];
        set => Variables[index] = value;
      }
      public Variable this[string name] {
        get {
          var Index = Variables.FindIndex(Entry => Entry.Name == name);
          return Index != -1 ? Variables[Index] : null;
        }
        set {
          var Index = Variables.FindIndex(Entry => Entry.Name == name);
          if(Index != -1)
            Variables[Index] = value;
        }
      }
      public int Count => Variables.Count;
      public bool IsReadOnly => ((ICollection<Variable>)Variables).IsReadOnly;
      public void Add(Variable item) {
        var Index = Variables.FindIndex(Entry => Entry.Name == item.Name);//変数名のインデックスを調べて
        if(Index == -1) {//未登録なら
          Variables.Add(new Variable(item.Name));//変数名を登録して
          Index = Variables.Count - 1;//そのインデックスを調べ
        }
        Variables[Index].Indices.Add(Index);//トークン中のインデックスを登録する
        Index = Variables.FindIndex(Entry => Entry.Name == "x");//変数名が「x」の位置を調べ
        if(Index != -1) {//見つかったら
          var Variable = Variables[Index];//「x」のパラメータを取り出し
          Variables.RemoveAt(Index);//一旦削除してから
          Variables.Insert(0, Variable);//先頭に差し込む
        }
      }
      public void AddRange(IEnumerable<Variable> items) => items.ToList().ForEach(item => Add(item));
      public void Clear() => Variables.Clear();
      public bool Contains(Variable item) => Variables.Contains(item);
      public bool Contains(string name) => Variables.FindIndex(Entry => Entry.Name == name) != -1;
      public void CopyTo(Variable[] array, int arrayIndex) => Variables.CopyTo(array, arrayIndex);
      public int IndexOf(Variable item) => Variables.IndexOf(item);
      public int IndexOf(string name) => Variables.FindIndex(Entry => Entry.Name == name);
      public void Insert(int index, Variable item) => Variables.Insert(index, item);
      public bool Remove(Variable item) => Variables.Remove(item);
      public bool Remove(string name) {
        var Index = Variables.FindIndex(Entry => Entry.Name == name);
        if(Index != -1)
          Variables.RemoveAt(Index);
        return Index != -1;
      }
      public void RemoveAt(int index) => Variables.RemoveAt(index);
      public IEnumerator<Variable> GetEnumerator() => Variables.GetEnumerator();
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)Variables).GetEnumerator();
      #endregion
    }

    //====================================================

    /// <summary>
    /// 文字列で与えられた数式を評価します
    /// </summary>
    /// <remarks>http://7ujm.net/etc/calcstart.html</remarks>
    public class Mathematical {
      /// <summary>
      /// 詳細モードを取得または設定します
      /// </summary>
      public bool IsVerbose { get; set; } = false;
      /// <summary>
      /// 設定された評価式を取得します
      /// </summary>
      public string Expression { get; private set; }
      /// <summary>
      /// トークンリスト
      /// </summary>
      public TokenCollection Tokens { get; private set; }
      /// <summary>
      /// パラメータリスト
      /// </summary>
      public VariableCollection Variables { get; private set; }

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="IsVerbose"></param>
      public Mathematical(string Expression, bool IsVerbose = false) {
        this.IsVerbose = IsVerbose;
        this.Expression = Regex.Replace(Expression.ToLower(), "\\s", "");

        Tokens = Postfixize(this.Expression);//トークンを取得
        Variables = DrawVariables(Tokens);//変数を取得
      }

      //----------------------------------------------------

      /// <summary>
      /// 関数名と必要なパラメータ数を保持します
      /// </summary>
      public static List<(string Name, int NumOfField)> Functions = new List<(string Name, int NumOfField)> {
        ("power", 2),("pow", 2),("mod", 2),("exp", 1),("log", 1),("ln", 1),("sqrt", 1),("abs", 1),
        ("sin", 1),("cos", 1),("tan", 1),("asin", 1),("acos", 1),("atan", 1),("sinh", 1),("cosh", 1),("tanh", 1),
        ("truncate", 1),("floor", 1),("ceiling",1),("round", 1),("sign", 1),
        ("pi", 0),("e", 0), ("c", 0), ("kb", 0), ("na", 0),
        ("sinc", 1),("decay", 3),("stretched", 3),("gauss", 3),("lorentz", 3),("foigt",4)
      };

      /// <summary>
      /// 演算子の優先度を返します
      /// </summary>
      /// <param name="Operator"></param>
      private int Priority(string Operator) {
        switch(Operator) {
          case "*": return 2;
          case "/": return 2;
          case "+": return 1;
          case "-": return 1;
          default: return 0;
        }
      }

      /// <summary>
      /// 数式を後置記法で取得します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="Indent"></param>
      /// <returns></returns>
      private TokenCollection Postfixize(string Expression, int Indent = 0) {
        var Defined = string.Join("|", Functions.Select(Function => Function.Name));
        var Stack = new System.Collections.Stack();//演算子のスタック
        var Postfix = new TokenCollection();//後置列
        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join("", Enumerable.Repeat("  ", Indent))} > {Expression}");

        #region 値・演算子・関数をトークンに分割する
        var Index = 0;//処理位置
        var Pivot = 0;//書き出し位置
        while(true) {
          var Character = Expression[Index].ToString();//1文字取り出す

          #region 括弧
          if(Character == "(") {//左括弧なら、対応する括弧の中をトークン化する
            #region 対応括弧範囲を調べて括弧内を取り出す
            var Brac = Index;//左括弧の位置
            var Ket = Brac + 1;//対応する右括弧の位置
            var Depth = 1;//対応括弧の深さ。すでに「(」が1つあるので1。0になったら対応する右括弧が見つかったことになる
            while(0 < Depth) {//対応する右括弧が見つかるまで
              if(Expression[Ket] == '(')//「(」があったら
                Depth++;//深さをインクリ
              else if(Expression[Ket] == ')')//「)」があったら
                Depth--;//深さをデクリ
              Ket++;//位置を右へずらす
            }
            Ket--;//位置を一つ戻すと対応する右括弧の位置
            var Local = Expression.Substring(Brac + 1, Ket - (Brac + 1));//括弧内を取り出す
            #endregion
            #region 後置記法列に追加する
            Postfix.AddRange(Postfixize(Local, Indent + 1));//括弧内をトークン化してから後置列に格納
            #endregion
            Index = Ket;//処理位置を右括弧に移動
            Pivot = Ket + 1;//書き出し位置を右括弧の次に移動
          }
          #endregion
          #region 関数
          else if(Regex.IsMatch(Character, "[a-zA-Z]")) {//アルファベットなら、関数または変数の先頭
            var Matched = Regex.Match(Expression.Substring(Index), "(?<name>[a-zA-Z]+)(?<delimitor>[\\*\\/\\+\\-\\(])");//処理位置以降の演算子または左括弧
            if(Matched.Success) {//区切り文字が見つかった
              var Name = Matched.Groups["name"].Value;
              if(Matched.Groups["delimitor"].Value == "(") {//見つかった区切りが左括弧なら関数
                if(Regex.IsMatch(Name, $"{Defined}")) {//定義された関数名なら
                  #region 対応括弧範囲を調べて括弧内を取り出す
                  var Brac = Index + Matched.Groups["delimitor"].Index;//処理位置以降の初めの左括弧の位置
                  var Ket = Brac + 1;//対応する右括弧の位置
                  var Depth = 1;//対応括弧の深さ。すでに「(」が1つあるので1。0になったら対応する右括弧が見つかったことになる
                  while(0 < Depth) {//対応する右括弧が見つかるまで
                    if(Expression[Ket] == '(')//「(」があったら
                      Depth++;//深さをインクリ
                    else if(Expression[Ket] == ')')//「)」があったら
                      Depth--;//深さをデクリ
                    Ket++;//位置を右へずらす
                  }
                  Ket--;//位置を一つ戻すと対応する右括弧の位置
                  var Local = Expression.Substring(Brac + 1, Ket - (Brac + 1));//括弧内を取り出す
                  #endregion
                  var NumOfField = Functions.Find(Function => Function.Name == Name).NumOfField;//必要なパラメータ数
                  if(0 < NumOfField) {
                    #region 関数のパラメータ区切りを調べる
                    var DelimitorIndices = new List<int>();//区切りコンマの位置
                    var Jndex = 0;//「Local」の処理位置
                    Depth = 0;//「Local」内の括弧や関数に含まれるコンマを無視する処理
                    while(true) {
                      if(Local[Jndex] == '(')
                        Depth++;
                      else if(Local[Jndex] == ')')
                        Depth--;
                      else if(Local[Jndex] == ',' & Depth == 0) {//見つかったコンマのDepthが0の時が現在処理中の関数のパラメータ区切り
                        DelimitorIndices.Add(Jndex);
                      }
                      if(DelimitorIndices.Count == NumOfField - 1)//区切りコンマの数が必要なパラメータ数に対応したら
                        break;
                      Jndex++;
                    }
                    DelimitorIndices.Add(Local.Length);//末尾の位置を区切りとして追加する
                    #endregion
                    #region 後置記法列に追加する
                    var Start = 0;//パラメータの先頭位置
                    for(int Kndex = 0; Kndex < DelimitorIndices.Count; Kndex++) {//区切り位置の数だけ
                      var Field = Local.Substring(Start, DelimitorIndices[Kndex] - Start);//「Local」からパラメータを取り出す
                      Postfix.AddRange(Postfixize(Field, Indent + 1));//パラメータをトークン化してから後置列に追加する
                      Start = DelimitorIndices[Kndex] + 1;//パラメータ先頭位置を区切り位置の次に移動
                    };
                  }
                  Postfix.Add(new Token("", TokenType.Function, Name, NumOfField));//関数名を後置列に追加
                  #endregion
                  Index = Ket;//処理位置を右括弧に移動
                  Pivot = Ket + 1;//書き出し位置を右括弧の次に移動
                } else //関数定義になければ
                  throw new Exception($"'{Name}' is not defined.");//例外を出す
              } else {//区切り文字が演算子なら変数
                Postfix.Add(new Token(Expression.Substring(Pivot, (Index - Pivot) + Matched.Groups["delimitor"].Index), TokenType.Variable, 0.0));//Pivotから演算子の手前までが変数
                Index += Matched.Groups["delimitor"].Index - 1;//変数名の長さ分だけ処理位置を移動
                Pivot = Index + 1;//書き出し位置を移動
              }
            } else {//区切り文字が見つからなかった
              Postfix.Add(new Token(Expression.Substring(Pivot), TokenType.Variable, 0.0));//末尾までが変数
              Index = Expression.Length - 1;//処理位置を末尾の手前まで移動
              Pivot = Index + 1;//書き出し位置を移動
            }
          }
          #endregion
          #region 数字
          else if(Regex.IsMatch(Character, "[0-9]")) {
            var Matched = Regex.Match(Expression.Substring(Index), "(?<value>^\\d(\\.[\\d]+([Ee][\\+\\-]?\\d+)?)?)");
            if(Matched.Success) {
              Postfix.Add(new Token("", TokenType.Numeric, double.Parse(Matched.Groups["value"].Value)));
              Index += Matched.Groups["value"].Length - 1;
              Pivot = Index + 1;
            }
          }
          #endregion
          #region 四則演算子
          else if(Regex.IsMatch(Character, "[\\*\\/\\+\\-]")) {//文字が演算子なら
            if(Index != 0) {//先頭以外のものを処理する
              if(Index != Pivot)//書き出し残しがあれば
                Postfix.Add(new Token("", TokenType.Numeric, double.Parse(Expression.Substring(Pivot, Index - Pivot))));//後置列に追加
              if(Stack.Count == 0)//スタックが空なら
                Stack.Push(Character);//スタックに積む
              else {//スタックに積まれていたら
                if(Priority((string)Stack.Peek()) < Priority(Character))//スタック先頭より優先度が高い演算子なら
                  Stack.Push(Character);//スタックに積む
                else {//スタック先頭と優先度が同じか低いなら
                  while(true) {
                    Postfix.Add(new Token("", TokenType.Operator, (string)Stack.Pop(), 2));//後置記法列に追加し
                    if(Stack.Count == 0 || Priority(Character) < Priority((string)Stack.Peek()))//スタックが空になるか、スタック先頭より優先度が低くなるまでくり返す
                      break;
                  }
                  Stack.Push(Character);//演算子をスタックに積む
                }
              }
              Pivot = Index + 1;
            } else {//先頭の
              if(Character == "-" && Regex.IsMatch(Expression.Substring(1, 1), "[a-zA-Z\\(]")) //次の文字がアルファベットか左括弧なら、先頭の「-」は符号反転演算子なので「-1*」にする
                Expression = Expression.Insert(Index + 1, "1*");
              Postfix.Add(new Token("", TokenType.Numeric, -1));
              Stack.Push("*");
              Index = 2;
              Pivot = Index + 1;
            }
          }
          #endregion

          #region 処理位置が末尾に到達したときの処理
          if(++Index == Expression.Length) {
            if(Index != Pivot)//書き出し残しがあれば
              Postfix.Add(new Token("", TokenType.Numeric, double.Parse(Expression.Substring(Pivot, Index - Pivot))));//後置列に追加
            while(Stack.Count != 0)//スタックに残った演算子をすべて
              Postfix.Add(new Token("", TokenType.Operator, (string)Stack.Pop(), 2));//後置記法列に追加
            break;
          }
          #endregion
        }
        #endregion

        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join("", Enumerable.Repeat("  ", Indent))} < {string.Join(",", Postfix.Select(Entry => Entry.Type == TokenType.Variable ? Entry.Name : Entry.Value))}");
        return Postfix;
      }

      /// <summary>
      /// トークン中の変数を取得します
      /// </summary>
      /// <param name="Tokens"></param>
      /// <returns></returns>
      private VariableCollection DrawVariables(TokenCollection Tokens) {
        var Variables = new List<Variable>();
        var Index = 0;
        foreach(var Item in Tokens.ToList().Where(Item => Item.Type == TokenType.Variable)) {//トークン中の変数について
          Index = Variables.FindIndex(Entry => Entry.Name == Item.Name);//変数名のインデックスを調べて
          if(Index == -1) {//未登録なら
            Variables.Add(new Variable(Item.Name));//変数名を登録して
            Index = Variables.Count - 1;//そのインデックスを調べ
          }
          Variables[Index].Indices.Add(Tokens.ToList().IndexOf(Item));//トークン中のインデックスを登録する
        }
        Index = Variables.FindIndex(Entry => Entry.Name == "x");//変数名が「x」の位置を調べ
        if(Index != -1) {//見つかったら
          var Variable = Variables[Index];//「x」のパラメータを取り出し
          Variables.RemoveAt(Index);//一旦削除してから
          Variables.Insert(0, Variable);//先頭に差し込む
        }
        return new VariableCollection(Variables);
      }

      //----------------------------------------------------

      /// <summary>
      /// 中置記法で再構成した数式を取得します
      /// </summary>
      public string Infix {
        get {
          if(0 < Tokens.Count) {
            var Rebuilt = Tokens.ToList();

            while(1 < Rebuilt.Count) {
              if(IsVerbose)
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join(",", Rebuilt.Select(Entry => Entry.Value))}");
              var Index = Rebuilt.FindIndex(v => v.Type != TokenType.Numeric & v.Type != TokenType.Variable);
              if(Index != -1) {
                var Operator = Rebuilt[Index];
                Token Processed = null;
                if(Operator.Type == TokenType.Operator)
                  Processed = new Token("", TokenType.Numeric, $@"({Rebuilt[Index - 2].Value}{Operator.Value}{Rebuilt[Index - 1].Value})");
                else if(Operator.Type == TokenType.Function)
                  Processed = new Token("", TokenType.Numeric, $@"{Operator.Value}({string.Join(",", Rebuilt.Skip(Index - Operator.NumberOfFields).Take(Operator.NumberOfFields).Select(v => v.Value))})");
                for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
                  Rebuilt.RemoveAt(Index - Operator.NumberOfFields);
                Rebuilt.Insert(Index - Operator.NumberOfFields, Processed);
              }
            }

            if(IsVerbose)
              Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join(",", Rebuilt.Select(Entry => Entry.Value))}");
            return (string)Rebuilt[0].Value;
          } else
            return "";
        }
      }

      /// <summary>
      /// 後置記法で再構成した数式を取得します
      /// </summary>
      public string Postfix => string.Join(",", Tokens.Select(Entry => Entry.Name != "" ? Entry.Name : Entry.Value));

      /// <summary>
      /// 数式を計算します
      /// </summary>
      /// <param name="X"></param>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      public double Evaluate(double X, IEnumerable<(string Name, double Value)> Parameters = null) {
        double a, b, c, d;//特殊関数用変数
        var Evaluators = this.Tokens.ToList();

        this.Variables["x"].Indices.ForEach(Index => Evaluators[Index].Value = X);
        Parameters.ToList().ForEach(Parameter => {
          this.Variables[Parameter.Name]?.Indices.ForEach(Index => Evaluators[Index].Value = Parameter.Value);
        });

        #region 計算を実行します
        while(1 < Evaluators.Count) {//トークンが1個になるまで
          var Operator = Evaluators.Find(Item => Item.Type == TokenType.Operator | Item.Type == TokenType.Function);//先頭の演算子または関数を探す
          var Index = Evaluators.IndexOf(Operator);//そのインデックス
          double Result = double.NaN;//計算結果のバッファ
          #region 四則演算
          if(Operator.Type == TokenType.Operator) //四則演算なら
            switch(Operator.Value) {
              case "+": Result = Evaluators[Index - 2].Value + Evaluators[Index - 1].Value; break;
              case "-": Result = Evaluators[Index - 2].Value - Evaluators[Index - 1].Value; break;
              case "*": Result = Evaluators[Index - 2].Value * Evaluators[Index - 1].Value; break;
              case "/": Result = Evaluators[Index - 1].Value != 0 ? Evaluators[Index - 2].Value / Evaluators[Index - 1].Value : double.NaN; break;
            }
          #endregion
          #region 関数計算
          if(Operator.Type == TokenType.Function) //関数なら
            switch(Operator.Value.ToLower()) {
              case "power":
              case "pow": Result = Math.Pow((double)Evaluators[Index - 2].Value, (double)Evaluators[Index - 1].Value); break;
              case "mod": Result = (double)Evaluators[Index - 1].Value != 0 ? (double)Evaluators[Index - 2].Value % (double)Evaluators[Index - 1].Value : double.NaN; break;
              case "exp": Result = Math.Exp((double)Evaluators[Index - 1].Value); break;
              case "log": Result = (double)Evaluators[Index - 1].Value > 0 ? Math.Log10((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "ln": Result = (double)Evaluators[Index - 1].Value > 0 ? Math.Log((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "sqrt": Result = (double)Evaluators[Index - 1].Value >= 0 ? Math.Sqrt((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "abs": Result = Math.Abs((double)Evaluators[Index - 1].Value); break;
              case "sin": Result = Math.Sin((double)Evaluators[Index - 1].Value); break;
              case "cos": Result = Math.Cos((double)Evaluators[Index - 1].Value); break;
              case "tan": Result = Math.Tan((double)Evaluators[Index - 1].Value); break;
              case "asin": Result = Math.Asin((double)Evaluators[Index - 1].Value); break;
              case "acos": Result = Math.Acos((double)Evaluators[Index - 1].Value); break;
              case "atan": Result = Math.Atan((double)Evaluators[Index - 1].Value); break;
              case "sinh": Result = Math.Sinh((double)Evaluators[Index - 1].Value); break;
              case "cosh": Result = Math.Cosh((double)Evaluators[Index - 1].Value); break;
              case "tanh": Result = Math.Tanh((double)Evaluators[Index - 1].Value); break;
              case "truncate": Result = Math.Truncate((double)Evaluators[Index - 1].Value); break;
              case "floor": Result = Math.Floor((double)Evaluators[Index - 1].Value); break;
              case "ceiling": Result = Math.Ceiling((double)Evaluators[Index - 1].Value); break;
              case "round": Result = Math.Round((double)Evaluators[Index - 1].Value); break;
              case "sign": Result = Math.Sign((double)Evaluators[Index - 1].Value); break;
              case "pi": Result = Math.PI; break;//円周率
              case "e": Result = Math.E; break;//自然対数の底
              case "c": Result = 2.99792458E8; break;//光速 m/s
              case "kb": Result = 1.380649E-23; break;//ボルツマン定数 J/K
              case "na": Result = 6.02214076E23; break;//アボガドロ数 /mol
              //以下は特殊処理
              case "sinc": Result = (double)Evaluators[Index - 1].Value != 0 ? Math.Sin((double)Evaluators[Index - 1].Value) / (double)Evaluators[Index - 1].Value : double.NaN; break;
              case "decay": Result = (double)Evaluators[Index - 1].Value != 0 ? (double)Evaluators[Index - 2].Value * Math.Exp(-(double)Evaluators[Index - 3].Value / (double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "stretched":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = b != 0 ? a * Math.Exp(-Math.Pow((double)Evaluators[Index - 4].Value, c) / b) : double.NaN;
                break;
              case "gauss":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = c != 0 ? a / (Math.Sqrt(2 * Math.PI) * c) * Math.Exp(-Math.Pow(((double)Evaluators[Index - 4].Value - b) / c, 2) / 2) : double.NaN;
                break;
              case "lorentz":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = a / Math.PI * c / (Math.Pow((double)Evaluators[Index - 4].Value - b, 2) + Math.Pow(c, 2));
                break;
              case "foigt":
                a = (double)Evaluators[Index - 4].Value;
                b = (double)Evaluators[Index - 3].Value;
                c = (double)Evaluators[Index - 2].Value;
                d = (double)Evaluators[Index - 1].Value;
                Result = a * (d / (Math.Sqrt(2 * Math.PI) * c) * Math.Exp(-Math.Pow(((double)Evaluators[Index - 5].Value - b) / c, 2) / 2) + //Gauss part
                         (1 - d) / Math.PI * c / (Math.Pow((double)Evaluators[Index - 5].Value - b, 2) + Math.Pow(c, 2))); //Lorentz part
                break;
            }
          #endregion
          for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
            Evaluators.RemoveAt(Index - Operator.NumberOfFields);
          Evaluators.Insert(Index - Operator.NumberOfFields, new Token("", TokenType.Numeric, Result));//計算結果を格納する
        }
        #endregion

        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | '{string.Join(",", this.Tokens.Select(Item => Item.Value))}' = {Evaluators[0].Value}");
        return Evaluators[0].Value;
      }

      /// <summary>
      /// 数式を計算します
      /// </summary>
      /// <param name="X"></param>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      public double Evaluate(double X, IEnumerable<double> Parameters = null) {
        double a, b, c, d;//特殊関数用変数
        var Evaluators = this.Tokens.ToList();

        this.Variables["x"].Indices.ForEach(Index => Evaluators[Index].Value = X);
        for(int Index=0; Index<Parameters.Count(); Index++)
          Evaluators[Index].Value = Parameters.ElementAt(Index);

        #region 計算を実行します
        while(1 < Evaluators.Count) {//トークンが1個になるまで
          var Operator = Evaluators.Find(Item => Item.Type == TokenType.Operator | Item.Type == TokenType.Function);//先頭の演算子または関数を探す
          var Index = Evaluators.IndexOf(Operator);//そのインデックス
          double Result = double.NaN;//計算結果のバッファ
          #region 四則演算
          if(Operator.Type == TokenType.Operator) //四則演算なら
            switch(Operator.Value) {
              case "+": Result = Evaluators[Index - 2].Value + Evaluators[Index - 1].Value; break;
              case "-": Result = Evaluators[Index - 2].Value - Evaluators[Index - 1].Value; break;
              case "*": Result = Evaluators[Index - 2].Value * Evaluators[Index - 1].Value; break;
              case "/": Result = Evaluators[Index - 1].Value != 0 ? Evaluators[Index - 2].Value / Evaluators[Index - 1].Value : double.NaN; break;
            }
          #endregion
          #region 関数計算
          if(Operator.Type == TokenType.Function) //関数なら
            switch(Operator.Value.ToLower()) {
              case "power":
              case "pow": Result = Math.Pow((double)Evaluators[Index - 2].Value, (double)Evaluators[Index - 1].Value); break;
              case "mod": Result = (double)Evaluators[Index - 1].Value != 0 ? (double)Evaluators[Index - 2].Value % (double)Evaluators[Index - 1].Value : double.NaN; break;
              case "exp": Result = Math.Exp((double)Evaluators[Index - 1].Value); break;
              case "log": Result = (double)Evaluators[Index - 1].Value > 0 ? Math.Log10((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "ln": Result = (double)Evaluators[Index - 1].Value > 0 ? Math.Log((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "sqrt": Result = (double)Evaluators[Index - 1].Value >= 0 ? Math.Sqrt((double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "abs": Result = Math.Abs((double)Evaluators[Index - 1].Value); break;
              case "sin": Result = Math.Sin((double)Evaluators[Index - 1].Value); break;
              case "cos": Result = Math.Cos((double)Evaluators[Index - 1].Value); break;
              case "tan": Result = Math.Tan((double)Evaluators[Index - 1].Value); break;
              case "asin": Result = Math.Asin((double)Evaluators[Index - 1].Value); break;
              case "acos": Result = Math.Acos((double)Evaluators[Index - 1].Value); break;
              case "atan": Result = Math.Atan((double)Evaluators[Index - 1].Value); break;
              case "sinh": Result = Math.Sinh((double)Evaluators[Index - 1].Value); break;
              case "cosh": Result = Math.Cosh((double)Evaluators[Index - 1].Value); break;
              case "tanh": Result = Math.Tanh((double)Evaluators[Index - 1].Value); break;
              case "truncate": Result = Math.Truncate((double)Evaluators[Index - 1].Value); break;
              case "floor": Result = Math.Floor((double)Evaluators[Index - 1].Value); break;
              case "ceiling": Result = Math.Ceiling((double)Evaluators[Index - 1].Value); break;
              case "round": Result = Math.Round((double)Evaluators[Index - 1].Value); break;
              case "sign": Result = Math.Sign((double)Evaluators[Index - 1].Value); break;
              case "pi": Result = Math.PI; break;//円周率
              case "e": Result = Math.E; break;//自然対数の底
              case "c": Result = 2.99792458E8; break;//光速 m/s
              case "kb": Result = 1.380649E-23; break;//ボルツマン定数 J/K
              case "na": Result = 6.02214076E23; break;//アボガドロ数 /mol
              //以下は特殊処理
              case "sinc": Result = (double)Evaluators[Index - 1].Value != 0 ? Math.Sin((double)Evaluators[Index - 1].Value) / (double)Evaluators[Index - 1].Value : double.NaN; break;
              case "decay": Result = (double)Evaluators[Index - 1].Value != 0 ? (double)Evaluators[Index - 2].Value * Math.Exp(-(double)Evaluators[Index - 3].Value / (double)Evaluators[Index - 1].Value) : double.NaN; break;
              case "stretched":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = b != 0 ? a * Math.Exp(-Math.Pow((double)Evaluators[Index - 4].Value, c) / b) : double.NaN;
                break;
              case "gauss":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = c != 0 ? a / (Math.Sqrt(2 * Math.PI) * c) * Math.Exp(-Math.Pow(((double)Evaluators[Index - 4].Value - b) / c, 2) / 2) : double.NaN;
                break;
              case "lorentz":
                a = (double)Evaluators[Index - 3].Value;
                b = (double)Evaluators[Index - 2].Value;
                c = (double)Evaluators[Index - 1].Value;
                Result = a / Math.PI * c / (Math.Pow((double)Evaluators[Index - 4].Value - b, 2) + Math.Pow(c, 2));
                break;
              case "foigt":
                a = (double)Evaluators[Index - 4].Value;
                b = (double)Evaluators[Index - 3].Value;
                c = (double)Evaluators[Index - 2].Value;
                d = (double)Evaluators[Index - 1].Value;
                Result = a * (d / (Math.Sqrt(2 * Math.PI) * c) * Math.Exp(-Math.Pow(((double)Evaluators[Index - 5].Value - b) / c, 2) / 2) + //Gauss part
                         (1 - d) / Math.PI * c / (Math.Pow((double)Evaluators[Index - 5].Value - b, 2) + Math.Pow(c, 2))); //Lorentz part
                break;
            }
          #endregion
          for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
            Evaluators.RemoveAt(Index - Operator.NumberOfFields);
          Evaluators.Insert(Index - Operator.NumberOfFields, new Token("", TokenType.Numeric, Result));//計算結果を格納する
        }
        #endregion

        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | '{string.Join(",", this.Tokens.Select(Item => Item.Value))}' = {Evaluators[0].Value}");
        return Evaluators[0].Value;
      }

    }

    //====================================================

    /// <summary>
    /// 文字列で与えられた論理式を評価します
    /// </summary>
    public class Logical {
      /// <summary>
      /// 詳細モードを取得または設定します
      /// </summary>
      public bool IsVerbose { get; set; } = false;
      /// <summary>
      /// 入力された式を取得します。
      /// </summary>
      public string Expression { get; private set; }
      /// <summary>
      /// 式の構成要素リストを取得します。
      /// </summary>
      public TokenCollection Tokens;
      /// <summary>
      /// 論理値の最終の値を保持します
      /// </summary>
      protected bool LatestValue = false;
      /// <summary>
      /// 比較値の変更を排他制御します
      /// </summary>
      private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

      //----------------------------------------------------

      /// <summary>
      /// 演算子の優先度を返します。
      /// </summary>
      /// <param name="Operator">演算子を指定します。</param>
      /// <returns>優先度を返します。</returns>
      private int Priority(string Operator) {
        switch(Operator) {
          case "!": return 3;//not
          case "&": return 2;//and
          case "|": return 1;//or
          default: return 0;
        }
      }

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public Logical() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="IsVerbose"></param>
      public Logical(string Expression, bool IsVerbose = false) {
        this.IsVerbose = IsVerbose;
        this.Expression = Regex.Replace(Expression, "\\s", "");

        Tokens = Postfixize(this.Expression);//トークンを取得
      }

      //----------------------------------------------------

      /// <summary>
      /// 論理式を後置記法で取得します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="Indent"></param>
      /// <returns></returns>
      private TokenCollection Postfixize(string Expression, int Indent = 0) {
        var Stack = new System.Collections.Stack();//演算子のスタック
        var Postfix = new TokenCollection();//後置列
        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join("", Enumerable.Repeat("  ", Indent))} > {Expression}");

        #region 値・演算子をトークンに分割する
        var Index = 0;//処理位置
        var Pivot = 0;//書き出し位置
        while(true) {
          var Character = Expression[Index].ToString();//1文字取り出す

          #region 括弧
          if(Character == "(") {//左括弧なら、対応する括弧の中をトークン化する
            #region 対応括弧範囲を調べて括弧内を取り出す
            var Brac = Index;//左括弧の位置
            var Ket = Brac + 1;//対応する右括弧の位置
            var Depth = 1;//対応括弧の深さ。すでに「(」が1つあるので1。0になったら対応する右括弧が見つかったことになる
            while(0 < Depth) {//対応する右括弧が見つかるまで
              if(Expression[Ket] == '(')//「(」があったら
                Depth++;//深さをインクリ
              else if(Expression[Ket] == ')')//「)」があったら
                Depth--;//深さをデクリ
              Ket++;//位置を右へずらす
            }
            Ket--;//位置を一つ戻すと対応する右括弧の位置
            var Local = Expression.Substring(Brac + 1, Ket - (Brac + 1));//括弧内を取り出す
            #endregion
            #region 後置記法列に追加する
            Postfix.AddRange(Postfixize(Local, Indent + 1));//括弧内をトークン化してから後置列に格納
            #endregion
            Index = Ket;//処理位置を右括弧に移動
            Pivot = Ket + 1;//書き出し位置を右括弧の次に移動
          }
          #endregion
          #region 論理演算子
          else if(Regex.IsMatch(Character, "[\\!\\&\\|]")) {//文字が演算子なら
            if(Index != 0) {//先頭以外のものを処理する
              if(Index != Pivot)//書き出し残しがあれば
                Postfix.Add(new Token(Expression.Substring(Pivot, Index - Pivot), TokenType.Logic, false));//後置列に追加
              if(Stack.Count == 0)//スタックが空なら
                Stack.Push(Character);//スタックに積む
              else {//スタックに積まれていたら
                if(Priority((string)Stack.Peek()) < Priority(Character))//スタック先頭より優先度が高い演算子なら
                  Stack.Push(Character);//スタックに積む
                else {//スタック先頭と優先度が同じか低いなら
                  while(true) {
                    Postfix.Add(new Token("", TokenType.Operator, (string)Stack.Peek(), (string)Stack.Pop() == "!" ? 1 : 2));//後置記法列に追加する。取りだした演算子が「!」ならフィールド数は1
                    if(Stack.Count == 0 || Priority(Character) < Priority((string)Stack.Peek()))//スタックが空になるか、スタック先頭より優先度が低くなるまでくり返す
                      break;
                  }
                  Stack.Push(Character);//演算子をスタックに積む
                }
              }
              Pivot = Index + 1;
            }
          }
          #endregion

          #region 処理位置が末尾に到達したときの処理
          if(++Index == Expression.Length) {
            if(Index != Pivot)//書き出し残しがあれば
              Postfix.Add(new Token(Expression.Substring(Pivot, Index - Pivot), TokenType.Logic, false));//後置列に追加
            while(Stack.Count != 0)//スタックに残った演算子をすべて
              Postfix.Add(new Token("", TokenType.Operator, (string)Stack.Peek(), (string)Stack.Pop() == "!" ? 1 : 2));//後置記法列に追加する。取りだした演算子が「!」ならフィールド数は1
            break;
          }
          #endregion
        }
        #endregion

        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join("", Enumerable.Repeat("  ", Indent))} < {string.Join(",", Postfix.Select(Entry => Entry.Value))}");
        return Postfix;
      }

      //----------------------------------------------------

      /// <summary>
      /// 最新の論理値を取得します
      /// </summary>
      public bool Value => LatestValue;

      /// <summary>
      /// 現在のオブジェクトを表す文字列を返します
      /// </summary>
      /// <returns></returns>
      public override string ToString() => $"'{Expression}'={LatestValue}";

      /// <summary>
      /// 中置記法で再構成した論理式を取得します
      /// </summary>
      public string Infix {
        get {
          //if(0 < Tokens.Count) {
          //  var Rebuilt = Tokens.ToList();

          //  while(1 < Rebuilt.Count) {
          //    if(IsVerbose)
          //      Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join(",", Rebuilt.Select(Entry => Entry.Value))}");
          //    var Index = Rebuilt.FindIndex(v => v.Type != TokenType.Numeric & v.Type != TokenType.Variable);
          //    if(Index != -1) {
          //      var Operator = Rebuilt[Index];
          //      Token Processed = null;
          //      if(Operator.Type == TokenType.Operator)
          //        Processed = new Token("", TokenType.Numeric, $@"({Rebuilt[Index - 2].Value}{Operator.Value}{Rebuilt[Index - 1].Value})");
          //      else if(Operator.Type == TokenType.Function)
          //        Processed = new Token("", TokenType.Numeric, $@"{Operator.Value}({string.Join(",", Rebuilt.Skip(Index - Operator.NumberOfFields).Take(Operator.NumberOfFields).Select(v => v.Value))})");
          //      for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
          //        Rebuilt.RemoveAt(Index - Operator.NumberOfFields);
          //      Rebuilt.Insert(Index - Operator.NumberOfFields, Processed);
          //    }
          //  }

          //  if(IsVerbose)
          //    Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | {string.Join(",", Rebuilt.Select(Entry => Entry.Value))}");
          //  return (string)Rebuilt[0].Value;
          //} else
          //  return "";
          return "";
        }
      }

      /// <summary>
      /// 後置記法で再構成した論理式を取得します
      /// </summary>
      public string Postfix => string.Join(",", Tokens.Select(Entry => Entry.Name != "" ? Entry.Name : Entry.Value));

      /// <summary>
      /// 論理式を評価してLatestValueに格納します
      /// </summary>
      protected virtual void Evaluate() {
        //Semaphore.Wait();
        var Evaluators = new TokenCollection(Tokens);

        //評価を実行します
        while(1 < Evaluators.Count) {//トークンが1個になるまで
          var Operator = Evaluators.Where(Token => Token.Type == TokenType.Operator).First();//先頭の演算子を探す
          var Index = Evaluators.IndexOf(Operator);
          bool Result = false;
          switch(Operator.Value) {
            case "!":
              Result = !(bool)Evaluators[Index - 1].Value;
              break;
            case "|":
              Result = (bool)Evaluators[Index - 2].Value | (bool)Evaluators[Index - 1].Value;
              break;
            case "&":
              Result = (bool)Evaluators[Index - 2].Value & (bool)Evaluators[Index - 1].Value;
              break;
          }
          for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
            Evaluators.RemoveAt(Index - Operator.NumberOfFields);
          Evaluators.Insert(Index - Operator.NumberOfFields, new Token("", TokenType.Logic, Result));
        }
        var OldValue = LatestValue;
        LatestValue = (bool)Evaluators[0].Value;
        //Semaphore.Release();
      }

      /// <summary>
      /// 比較値を変更して論理値の結果を取得します
      /// </summary>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      /// <summary>
      public bool Evaluate(IEnumerable<(string Expression, bool Value)> Parameters) {
        if(Parameters != null & Tokens.Count != 0) {
          foreach(var Item in Parameters.ToList())//各比較式の値をセット
            Tokens[Item.Expression].Value = Item.Value;
          Evaluate();
          return LatestValue;
        } else
          return false;
      }

    }

    /// <summary>
    /// 比較式を提供します
    /// </summary>
    public class Comparable : Variable, IDisposable {
      /// <summary>
      /// 左辺の変数を取得または設定します
      /// </summary>
      public Variable Left { get; set; } = null;
      /// <summary>
      /// 右辺の変数を取得または設定します
      /// </summary>
      public Variable Right { get; set; } = null;
      /// <summary>
      /// 比較演算子を取得します
      /// </summary>
      public string Comparator { get; private set; } = "";
      /// <summary>
      /// 比較式を取得します
      /// </summary>
      public string Expression { get; private set; } = "";
      /// <summary>
      /// 比較値の変化を通知します
      /// </summary>
      public new event EventHandler<DateTime> OnChanged;
      /// <summary>
      /// 比較値の更新を通知します
      /// </summary>
      public new event EventHandler<DateTime> OnUpdated;
      /// <summary>
      /// 比較値の変更を排他制御します
      /// </summary>
      private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public Comparable() : base() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="Parameters"></param>
      /// <param name="IsVerbose"></param>
      public Comparable(string Expression, List<Variable> Parameters, bool IsVerbose = false) : base(Expression, IsVerbose) {
        var Matched = Regex.Match(Expression, "(?<left>[^\\<\\>\\=\\!]+)((?<comparator>(\\<\\=?|\\>\\=?|\\=\\=?|\\!\\=))(?<right>[^\\<\\>\\=\\!]+))?");//式を分解し
        if(Matched.Success) {
          Comparator = Matched.Groups["comparator"].Value != "" ? Matched.Groups["comparator"].Value : "==";
          var LeftIndex = Parameters.FindIndex(Parameter => Parameter.Name == Matched.Groups["left"].Value);//左辺の名前のパラメータを検索
          var RightIndex = Parameters.FindIndex(Parameter => Parameter.Name == Matched.Groups["right"].Value);//右辺の名前のパラメータを検索
          #region 両辺が定数
          if(LeftIndex == -1 & RightIndex == -1) {//両辺が定数
            throw new Exception("No variable found.");
          }
          #endregion
          #region 左辺が変数、右辺が定数
          else if(LeftIndex != -1 & RightIndex == -1) {//左辺が変数、右辺が定数
            Left = new Variable(Matched.Groups["left"].Value, Parameters[LeftIndex].Value, IsVerbose);
            Parameters[LeftIndex].OnChanged += Variable_OnChanged;//変更イベントハンドラを登録
            Parameters[LeftIndex].OnUpdated += Variable_OnUpdated;//更新イベントハンドラを登録
            if(Parameters[LeftIndex].Value.GetType() == typeof(double))
              Right = new Variable("", double.Parse(Matched.Groups["right"].Value), IsVerbose);
            else if(Parameters[LeftIndex].Value.GetType() == typeof(DateTime))
              Right = new Variable("", DateTime.Parse(Matched.Groups["right"].Value), IsVerbose);
            else if(Parameters[LeftIndex].Value.GetType() == typeof(bool))
              Right = new Variable("", Matched.Groups["right"].Value != "" ? bool.Parse(Matched.Groups["right"].Value) : true, IsVerbose);
          }
          #endregion
          #region 左辺が定数、右辺が変数
          else if(LeftIndex == -1 & RightIndex != -1) {//左辺が定数、右辺が変数
            Right = new Variable(Matched.Groups["right"].Value, Parameters[RightIndex].Value, IsVerbose);
            Parameters[RightIndex].OnChanged += Variable_OnChanged;//変更イベントハンドラを登録
            Parameters[RightIndex].OnUpdated += Variable_OnUpdated;//更新イベントハンドラを登録
            if(Matched.Groups["left"].Value != "") {
              if(Parameters[RightIndex].Value.GetType() == typeof(double))
                Left = new Variable("", double.Parse(Matched.Groups["left"].Value), IsVerbose);
              else if(Parameters[RightIndex].Value.GetType() == typeof(DateTime))
                Left = new Variable("", DateTime.Parse(Matched.Groups["left"].Value), IsVerbose);
              else if(Parameters[RightIndex].Value.GetType() == typeof(bool))
                Left = new Variable("", bool.Parse(Matched.Groups["left"].Value), IsVerbose);
            }
          }
          #endregion
          #region 両辺が変数
          else if(LeftIndex != -1 & RightIndex != -1) {//両辺が変数
            Left = new Variable(Matched.Groups["left"].Value, Parameters[LeftIndex].Value, IsVerbose);
            Parameters[LeftIndex].OnChanged += Variable_OnChanged;//変更イベントハンドラを登録
            Parameters[LeftIndex].OnUpdated += Variable_OnUpdated;//更新イベントハンドラを登録
            Right = new Variable(Matched.Groups["right"].Value, Parameters[RightIndex].Value, IsVerbose);
            Parameters[RightIndex].OnChanged += Variable_OnChanged;//変更イベントハンドラを登録
            Parameters[RightIndex].OnUpdated += Variable_OnUpdated;//更新イベントハンドラを登録
          }
          #endregion
        } else
          throw new Exception($"{DateTime.Now:HH:mm:ss.ff} | Expression '{Expression}' incorrect. ");
        Evaluate();
      }

      /// <summary>
      /// 破棄します
      /// </summary>
      public void Dispose() {
        Left.OnChanged -= Variable_OnChanged;
        Right.OnChanged -= Variable_OnChanged;
      }

      //----------------------------------------------------

      /// <summary>
      /// パラメータ値の変更を受信して最新の比較値をLatestValueに格納し、イベントを送信します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Variable_OnChanged(object sender, DateTime e) {
        var OldValue = base.Value;
        if(Left.Name == ((Variable)sender).Name)
          Left.SetValue(((Variable)sender).Value);
        if(Right.Name == ((Variable)sender).Name)
          Right.SetValue(((Variable)sender).Value);
        Evaluate();
        if(OldValue != base.Value)
          OnChanged?.Invoke(this, DateTime.Now);
        else
          OnUpdated?.Invoke(this, DateTime.Now);
      }

      /// <summary>
      /// パラメータ値の更新を受信し、イベントを送信します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Variable_OnUpdated(object sender, DateTime e) {
        OnUpdated?.Invoke(this, DateTime.Now);
      }

      //----------------------------------------------------

      /// <summary>
      /// 両辺の値を比較してLatestValueに格納します
      /// </summary>
      protected void Evaluate() {
        //Semaphore.Wait();
        var Result = false;
        switch(this.Comparator) {//比較演算子の種類に応じて比較を行う
          case "=":
          case "==":
            Result = Left.Value.CompareTo(Right.Value) == 0;
            break;
          case "!=":
            Result = Left.Value.CompareTo(Right.Value) != 0;
            break;
          case ">":
            Result = Left.Value.CompareTo(Right.Value) > 0;
            break;
          case ">=":
            Result = Left.Value.CompareTo(Right.Value) >= 0;
            break;
          case "<":
            Result = Left.Value.CompareTo(Right.Value) < 0;
            break;
          case "<=":
            Result = Left.Value.CompareTo(Right.Value) <= 0;
            break;
        }
        base.Value = Result;
        //Semaphore.Release();
      }

      /// <summary>
      /// パラメータ値を変更して比較値の結果を取得します
      /// </summary>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      public bool Evaluate(IEnumerable<Variable> Parameters) {
        if(Parameters != null)
          foreach(var Parameter in Parameters.ToList()) {
            if(Parameter.Name == Left.Name)
              Left.Value = Parameter.Value;
            else if(Parameter.Name == Right.Name)
              Right.Value = Parameter.Value;
          }
        Evaluate();
        return base.Value;
      }

      /// <summary>
      /// パラメータ値を変更して比較値の結果を取得します
      /// </summary>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      public bool Evaluate(IEnumerable<(string Name, dynamic Value)> Parameters) {
        if(Parameters != null)
          foreach(var Parameter in Parameters.ToList()) {
            if(Parameter.Name == Left.Name)
              Left.Value = Parameter.Value;
            else if(Parameter.Name == Right.Name)
              Right.Value = Parameter.Value;
          }
        Evaluate();
        return base.Value;
      }

    }

    /// <summary>
    /// 文字列で与えられた論理式を評価して通知します
    /// </summary>
    public class Trigger : Logical, IDisposable {
      /// <summary>
      /// 値の更新を通知します
      /// </summary>
      public event EventHandler<DateTime> OnUpdated;
      /// <summary>
      /// 値の変化を通知します
      /// </summary>
      public event EventHandler<DateTime> OnChanged;
      /// <summary>
      /// 比較値の変更を排他制御します
      /// </summary>
      private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public Trigger() : base() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Expression"></param>
      /// <param name="Parameters"></param>
      /// <param name="IsVerbose"></param>
      public Trigger(string Expression, IEnumerable<Variable> Parameters, bool IsVerbose = false) : base(Expression, IsVerbose) {
        foreach(var Token in Tokens.Where(Token => Token.Type == TokenType.Logic)) {//トークン中の論理式について
          Token.Value = new Comparable(Token.Name, Parameters.ToList(), IsVerbose);
          ((Comparable)Token.Value).OnChanged += Comparable_OnChanged;
          ((Comparable)Token.Value).OnUpdated += Comparable_OnUpdated;
        }
      }

      /// <summary>
      /// 破棄します
      /// </summary>
      public void Dispose() {
        foreach(var Token in Tokens.Where(Token => Token.Type == TokenType.Logic)) {//トークン中の論理式について
          ((Comparable)Token.Value).OnChanged -= Comparable_OnChanged;
          ((Comparable)Token.Value).OnUpdated -= Comparable_OnUpdated;
        }
      }

      //----------------------------------------------------

      /// <summary>
      /// 論理式を評価してLatestValueに格納します
      /// </summary>
      protected override void Evaluate() {
        //Semaphore.Wait();
        var Evaluators = new TokenCollection();
        foreach(var Token in Tokens)
          if(Token.Type == TokenType.Operator)
            Evaluators.Add(Token);
          else if(Token.Type == TokenType.Logic)
            Evaluators.Add(new Token(((Comparable)Token.Value).Expression, TokenType.Logic, ((Comparable)Token.Value).Value));

        //評価を実行します
        while(1 < Evaluators.Count) {//トークンが1個になるまで
          var Operator = Evaluators.Where(Token => Token.Type == TokenType.Operator).First();//先頭の演算子を探す
          var Index = Evaluators.IndexOf(Operator);
          bool Result = false;
          switch(Operator.Value) {
            case "!":
              Result = !(bool)Evaluators[Index - 1].Value;
              break;
            case "|":
              Result = (bool)Evaluators[Index - 2].Value | (bool)Evaluators[Index - 1].Value;
              break;
            case "&":
              Result = (bool)Evaluators[Index - 2].Value & (bool)Evaluators[Index - 1].Value;
              break;
          }
          for(int Jndex = 0; Jndex <= Operator.NumberOfFields; Jndex++)
            Evaluators.RemoveAt(Index - Operator.NumberOfFields);
          Evaluators.Insert(Index - Operator.NumberOfFields, new Token("", TokenType.Logic, Result));
        }

        var OldValue = LatestValue;
        LatestValue = Evaluators[0].Value;
        //Semaphore.Release();
      }

      /// <summary>
      /// 比較値を変更して論理値の結果を取得します
      /// </summary>
      /// <param name="Parameters"></param>
      /// <returns></returns>
      public bool Evaluate(IEnumerable<Comparable> Parameters) {
        if(Parameters != null & Tokens.Count != 0) {
          foreach(var Item in Parameters.ToList())//各比較式の値をセット
            Tokens[Item.Comparator].Value = Item.Value;
          Evaluate();
        }
        return this.LatestValue;
      }

      /// <summary>
      /// 比較値の変更を受信して最新の論理値をLatestValueに格納し、イベントを送信します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Comparable_OnChanged(object sender, DateTime e) {
        var OldValue = base.Value;
        Evaluate();
        if(OldValue != base.Value) {
          if(IsVerbose)
            Console.WriteLine($"{e:HH:mm:ss.ff} | '{Expression}' changed {OldValue} -> {LatestValue}");
          OnChanged?.Invoke(this, e);
        }
      }

      /// <summary>
      /// 比較値の更新を受信し、イベントを送信します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Comparable_OnUpdated(object sender, DateTime e) {
        OnUpdated?.Invoke(this, e);
      }

    }

    //====================================================

    /// <summary>
    /// 使い方のデモを提供します
    /// </summary>
    public static class Demo {

      /// <summary>
      /// Mathematicalのデモを行います
      /// </summary>
      public static void Mathematical(bool IsVerbose = false) {
        var Expression = "(height-offset)*exp(-x/4.53E-13)+offset";
        var WorkExpression = Regex.Replace(Expression.ToLower(), "\\s", "");//小文字化して空白を除去する。日時形式の文字列は想定していない
        var Calculator = new Mathematical(WorkExpression, IsVerbose);

        if(IsVerbose) {
          Console.WriteLine(Calculator.Postfix);
          Console.WriteLine(Calculator.Infix);
          foreach(var Item in Calculator.Tokens)
            Console.WriteLine(Item);
        }

        var Result = Calculator.Evaluate(3.0, new List<(string, double)> { ("height", 5.0), ("decay", 1.5), ("offset", 0.5) });

        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | '{Expression}' = {Result}\n");
      }

      /// <summary>
      /// Logicalのデモを行います
      /// </summary>
      public static void Logic(bool IsVerbose = false) {
        var Expression = "TickTimer>=5 | 25.5<Temperature | !(WaterLevel>=1.05E+2) | 2021/09/30T23:00:00<Stamp";
        var WorkExpression = Regex.Replace(Expression, "\\s", "");//空白を除去する
        var Verifier = new Logical(WorkExpression, IsVerbose);

        if(IsVerbose) {
          Console.WriteLine(Verifier.Postfix);
          Console.WriteLine(Verifier.Infix);
          foreach(var Item in Verifier.Tokens)
            Console.WriteLine(Item);
        }

        var Result = Verifier.Evaluate(new List<(string, bool)> {
        ("TickTimer>=5",false),
        ("25.5<Temperature",false),
        ("WaterLevel>=1.05E+2",false),
        ("2021/09/30T23:00:00<Stamp",true)
      });

        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | '{Expression}' = {Result}\n");
      }

      //----------------------------------------------------

      #region Triggerデモで使用する変数
      static System.Timers.Timer Timer = new System.Timers.Timer(500);//タイマを保持します
      static DateTime Origin = DateTime.Now;//開始時刻を保持します
      static Random Randomizer = new Random((int)DateTime.Now.ToOADate());//乱数発生器を保持します

      static Variable TickTime = new Variable("TickTime", 0.0);//経過時間を保持します
      static Variable Temperature = new Variable("Temperature", 30.0);//温度値を保持します
      static Variable WaterLevel = new Variable("WaterLevel", 0.0);//水位値を保持します
      static ManualResetEvent Completed = new ManualResetEvent(false);//リセットイベントを保持します
      #endregion

      /// <summary>
      /// Triggerのデモを行います
      /// </summary>
      public static void Trigger(bool IsVerbose = false) {
        Timer.Elapsed += Timer_Elapsed;//タイマイベントハンドラを登録します

        TickTime.IsVerbose = IsVerbose;
        Temperature.IsVerbose = IsVerbose;
        WaterLevel.IsVerbose = IsVerbose;
        var Trigger = new Trigger(
          "(WaterLevel>=5|Temperature<20.0)&10<TickTime",
          new List<Variable> { WaterLevel, Temperature, TickTime },
          IsVerbose);//トリガを生成します
        Trigger.OnChanged += Trigger_OnChanged;//トリガイベントハンドラを登録します

        if(IsVerbose)
          Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | Trigger started");

        Timer.Start();//タイマを開始します
        Completed.WaitOne();//トリガイベントを待機します
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} | Triggered\n");

        Trigger.OnChanged -= Trigger_OnChanged;//トリガイベントハンドラを解除します
      }

      /// <summary>
      /// タイマイベントを処理します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
        Console.WriteLine();
        TickTime.Value = (DateTime.Now - Origin).TotalSeconds;//経過時間を更新します
        Temperature.Value -= Randomizer.NextDouble();//温度を更新します
        WaterLevel.Value += Randomizer.NextDouble();//水位を更新します
      }

      /// <summary>
      /// トリガーイベントを処理します
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      static void Trigger_OnChanged(object sender, DateTime e) {
        Timer.Stop();//タイマを停止します
        Completed.Set();//リセットイベントをセットします
      }

    }

  }

}
