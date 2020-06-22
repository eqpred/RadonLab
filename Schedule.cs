using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;

namespace RadonLab {

	namespace Schedule {
		/// <summary>Channelクラスを継承したオブジェクトに対して自動処理を行う機能を提供します。</summary>
		[System.Runtime.CompilerServices.CompilerGenerated]
    internal class NamespaceDoc { }

		//====================================================================================
		/// <summary>
		/// 与えられた単一の評価式が満たされたかどうかを評価します。
		/// </summary>
		/// <example>
		/// Validationの使用例を示します。複数の評価式を組み合わせた論理式の評価は<see cref="LogicExpression"/>で行います。インスタンスの検索には SampleBuffer.Utilities.GetInstance を使っています。
		/// <code>
		/// void TestValidation() {
		///   string Expression = "TickTimer&gt;5";//評価式を設定します。TickTimerという名称のインスタンスがあって、その値が5を超えるかどうかを評価します。
		///   Expression.Validation Validator = new Expression.Validation(Expression, new object[] { this });//評価式のインスタンスTickTimerの親オブジェクトがアプリケーションなので「this」を指定しています。Ownersに複数のオブジェクトを指定すると、その中から検索を行います。
		///   if(Validator != null) {
		///     Validator.IsActive = true;//Validatorを有効にします。
		///   
		///     while(Validator.Value) {//Validatorの値がtrueになるまでループします。
		///       System.Threading.Thread.Sleep(100);//100ミリ秒ウェイトします。
		///     
		///     Validator.IsActive = false;//Validatorを無効にします。
		///   }
		/// </code>
		/// </example>
		public class Validation : Channel, IHandler {
			#region フィールド

			//------------------------------
			/// <summary>
			/// 評価式を取得します。
			/// </summary>
			public string Expression { get; protected set; }

			//------------------------------
			/// <summary>
			/// 評価式の左辺の値を取得します。
			/// </summary>
			public dynamic LeftInstance { get; protected set; }

			//------------------------------
			/// <summary>
			/// 評価式の比較演算子を取得します。
			/// </summary>
			public Expressions.ComparatorType Comparator { get; protected set; }

			//------------------------------
			/// <summary>
			/// 評価式の右辺の値を取得します。
			/// </summary>
			public dynamic RightInstance { get; protected set; }

			//------------------------------
			/// <summary>
			/// 評価式の各辺の最新値を保持します。
			/// </summary>
			dynamic LeftLatestValue, RightLatestValue;

			#endregion

			#region メソッド

			//------------------------------
			/// <summary>
			/// Validationを初期化します。
			/// </summary>
			/// <param name="Owner">評価式が要求するインスタンスの親オブジェクトを指定します。</param>
			/// <param name="Expression">単一の評価式を指定します。</param>
			/// <param name="IsVerbose">詳細モードを指定します。</param>
			public Validation(object Owner, string Expression, bool IsVerbose = false) : base("Validator", "Validator", false, IsVerbose) {
				const string Logic = @"(?<logic>(?<left>[\w:]*)?(?<comparator>(=|==|>|>=|<|<=|!=){1})(?<right>[\w\.]+))";

				if(Owner != null & Expression != "") {
					Message = new EventMessage() { Command = "Validated", Source = Expression };
					LatestValue = false;

					//評価式要素の判別
					var Matched = Regex.Match(Regex.Replace(Expression, @"\s", ""), Logic, RegexOptions.IgnoreCase);//条件式の空白を削除してから評価式を分割する
					this.Expression = Matched.Value;//最初にマッチしたものについて評価する
					if(IsVerbose)
						Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | {this.Expression} を評価します。 @Validation");

					LeftInstance = RadonLab.Utilities.GetInstance(Owner, Matched.Groups["left"].Value, typeof(Channel));//インスタンスを取得する。フィールドはメイン内にあるもの
					RightInstance = RadonLab.Utilities.GetInstance(Owner, Matched.Groups["right"].Value, typeof(Channel));//インスタンスを取得する。フィールドはメイン内にあるもの
					Comparator = GetComparatorType(Matched.Groups["comparator"].Value);//比較演算子

					//論理式の構成に応じて設定を行う
					if(LeftInstance != null & RightInstance != null) {//どちらも変数という場合は型が比較できるものかを調べる必要がある
						var LeftType = LeftInstance.GetType().GetProperty("Value").PropertyType;
						var RightType = RightInstance.GetType().GetProperty("Value").PropertyType;
						if(LeftType != RightType)
							throw new Exception($@"指定した変数の型が一致しません。左辺={LeftType.Name},右辺={RightType.Name}");
					} else if(LeftInstance != null & RightInstance == null) {//左辺が変数、右辺が定数 
						RightInstance = Matched.Groups["right"].Value;//定数文字列を入れておく
					} else if(LeftInstance == null & RightInstance != null) {//左辺が定数、右辺が変数
						LeftInstance = Matched.Groups["left"].Value;//定数文字列を入れておく
					} else if(LeftInstance == null & RightInstance == null) {//どちらも定数の場合は無限ループに入るのを防ぐためLatestStatをtrueにする
						LatestValue = true;
					}

				}
			}

			//------------------------------
			/// <summary>
			/// 評価式の比較子を判別します。
			/// </summary>
			/// <param name="ComparatorElement">比較子を指定します。</param>
			/// <returns>比較子の型を返します。</returns>
			private Expressions.ComparatorType GetComparatorType(string ComparatorElement) {
				Expressions.ComparatorType Comparator;
				switch(ComparatorElement) {//評価式の比較演算子文字列
					case "==":
					case "=":
						Comparator = Expressions.ComparatorType.Equal;
						break;
					case "!=":
						Comparator = Expressions.ComparatorType.NotEqual;
						break;
					case ">":
						Comparator = Expressions.ComparatorType.Larger;
						break;
					case ">=":
						Comparator = Expressions.ComparatorType.LargerThan;
						break;
					case "<":
						Comparator = Expressions.ComparatorType.Lower;
						break;
					case "<=":
						Comparator = Expressions.ComparatorType.LowerThan;
						break;
					default:
						Comparator = Expressions.ComparatorType.Null;
						break;
				}
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | 比較子は {Comparator} です。 @Validation");
				return Comparator;
			}

			//------------------------------
			/// <summary>
			/// 評価を停止または開始します。
			/// </summary>
			public override bool IsActive {
				get => base.IsActive;
				set {
					if(value != base.IsActive) {
						if(value) {
							if(!(LeftInstance == null & RightInstance == null)) {
								//論理式の構成に応じて設定を行う
								//　どちらも変数の場合　----------------------------
								if(LeftInstance.GetType().IsSubclassOf(typeof(Channel)) & RightInstance.GetType().IsSubclassOf(typeof(Channel))) {
									((Channel)this.LeftInstance).Subscribe(this.HandleMessage);//変数の通知先に登録する
									((Channel)this.RightInstance).Subscribe(this.HandleMessage);//変数の通知先に登録する
								} else
								//　左辺が変数、右辺が定数　----------------------------
								if(LeftInstance.GetType().IsSubclassOf(typeof(Channel)) & RightInstance.GetType().Equals(typeof(string))) {
									((Channel)this.LeftInstance).Subscribe(this.HandleMessage);//変数の通知先に登録する
									var LeftType = ((Channel)this.LeftInstance).Value.GetType();//左辺の型を調べ // LeftInstance.GetType().GetProperty("LatestValue").PropertyType;
									if(LeftType.Equals(typeof(DateTime))) {//指定の型がDateTimeで
										if(DateTime.TryParse((string)RightInstance, out DateTime ResultDateTime)) {//DateTimeで変換できたら
											RightInstance = ResultDateTime;//指定日付時刻を定数にする
										} else if(double.TryParse((string)RightInstance, out double ResultDouble)) {//数値型で変換できたら
											var sec = (int)Math.Round(ResultDouble, 0);
											var msec = (int)(ResultDouble - sec) * 1000;
											var ts = new TimeSpan(0, 0, 0, sec, msec);
											RightInstance = DateTime.Now + ts;//現在時刻からの経過秒数後の時刻を定数にする
										}
									} else if(LeftType.Equals(typeof(double)) | LeftType.Equals(typeof(int))) {//指定の型が数値なら
										if(double.TryParse((string)RightInstance, out double ResultDouble)) {//doubleで変換できたら
											RightInstance = ResultDouble;//変換値を定数にする
										}
									} else if(LeftType.Equals(typeof(bool))) {//指定の型がboolなら
										if(bool.TryParse((string)RightInstance, out bool ResultBool)) {//doubleで変換できたら
											RightInstance = ResultBool;
										}
									}//指定の型が「Datetime,int,double,bool」以外なら文字として処理する
									RightLatestValue = RightInstance;//最新値を設定して
								} else
								//　左辺が定数、右辺が変数　----------------------------
								if(LeftInstance.GetType().Equals(typeof(string)) & RightInstance.GetType().IsSubclassOf(typeof(Channel))) {
									((Channel)this.RightInstance).Subscribe(this.HandleMessage);//変数の通知先に登録する
									var RightType = ((Channel)this.LeftInstance).Value.GetType(); //.GetProperty("Value").PropertyType;
									if(RightType.Equals(typeof(DateTime))) {//指定の型がDateTimeで
										if(DateTime.TryParse((string)LeftInstance, out DateTime ResultDateTime)) {//DateTimeで変換できたら
											LeftInstance = ResultDateTime;//指定日付時刻を定数にする
										} else if(double.TryParse((string)LeftInstance, out double ResultDouble)) {//数値型で変換できたら
											var sec = (int)Math.Round(ResultDouble, 0);
											var msec = (int)(ResultDouble - sec) * 1000;
											var ts = new TimeSpan(0, 0, 0, sec, msec);
											LeftInstance = DateTime.Now + ts;//現在時刻からの経過秒数後の時刻を定数にする
										}
									} else if(RightType.Equals(typeof(double)) | RightType.Equals(typeof(int))) {//指定の型が数値なら
										if(double.TryParse((string)LeftInstance, out double ResultDouble)) {//doubleで変換できたら
											LeftInstance = ResultDouble;//変換値を定数にする
										}
									} else if(RightType.Equals(typeof(bool))) {//指定の型がboolなら
										if(bool.TryParse((string)LeftInstance, out bool ResultBool)) {//doubleで変換できたら
											LeftInstance = ResultBool;
										}
									}//指定の型が「Datetime,int,double,bool」以外なら文字として処理する
									LeftLatestValue = LeftInstance;//最新値を設定して
								}
							}
							//それ以外は無限ループに入るのを防ぐためLatestStatをtrueにする
							else {
								LatestValue = true;
							}
							base.IsActive = true;//評価を開始する
							if(IsVerbose)
								Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | {this.Expression} の評価を開始しました。L=""{this.LeftInstance}"", R=""{this.RightInstance}"". @Validation");
						} else {
							base.IsActive = false;
							//左辺の処理
							if(this.LeftInstance.GetType().IsSubclassOf(typeof(Channel)))//左辺が変数なら
								((Channel)this.LeftInstance).Unsubscribe(this.HandleMessage);//変数の通知先から削除する
																																						 //右辺の処理
							if(this.RightInstance.GetType().IsSubclassOf(typeof(Channel)))//右辺が変数なら
								((Channel)this.RightInstance).Unsubscribe(this.HandleMessage);//変数の通知先から削除する
							if(IsVerbose)
								Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | {this.Expression} の評価を停止しました。 @Validation");
						}
					}
				}
			}

			//------------------------------
			/// <summary>
			/// 式を評価します。
			/// </summary>
			private void Evaluate() {
				if(IsActive) {
					bool Satisfied = false;
					switch(Comparator) {
						case Expressions.ComparatorType.Equal:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) == 0;
							break;
						case Expressions.ComparatorType.NotEqual:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) != 0;
							break;
						case Expressions.ComparatorType.Larger:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) > 0;
							break;
						case Expressions.ComparatorType.LargerThan:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) >= 0;
							break;
						case Expressions.ComparatorType.Lower:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) < 0;
							break;
						case Expressions.ComparatorType.LowerThan:
							Satisfied = LeftLatestValue.CompareTo(RightLatestValue) <= 0;
							break;
					}

					if(IsVerbose)
						Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | [{LeftLatestValue}] {Comparator} [{RightLatestValue}] : {(LatestValue == Satisfied ? "状態変化を待っています。" : "状態変化を捕捉しました。")} @Validation");

					if(LatestValue != Satisfied) {
						if(IsVerbose)
							Console.WriteLine($@"{DateTime.Now.ToString(RadonLab.Formats.Stamp)} | {this.Expression} が達成されました。 @Validation");
						LatestValue = Satisfied;
						this.Message.Stamp = DateTime.Now;
						this.Message.Value = LatestValue;
						Notify(this, this.Message);
					}
				}
			}

			//------------------------------
			/// <summary>
			/// 左辺の最新値を取得または設定します。
			/// </summary>
			public dynamic LeftValue {
				get { return LeftLatestValue; }
				set {
					LeftLatestValue = value;
					Evaluate();
				}
			}

			//------------------------------
			/// <summary>
			/// 右辺の最新値を取得または設定します。
			/// </summary>
			public dynamic RightValue {
				get { return RightLatestValue; }
				set {
					RightLatestValue = value;
					Evaluate();
				}
			}

			//------------------------------
			/// <summary>
			/// 変数からのメッセージを処理します。
			/// </summary>
			/// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
			/// <param name="Message">EventMessageが格納されています。</param>
			public override void HandleMessage(object Sender, EventMessage Message) {
				if(IsActive) {
					if(IsVerbose)
						Console.WriteLine($@"{Message.Stamp.ToString(RadonLab.Formats.Stamp)} | {Message.Source} からの {Message.Command} を捕捉しました。 @Validation");
					if(Sender.Equals(this.LeftInstance))
						LeftValue = Message.Value;
					if(Sender.Equals(this.RightInstance))
						RightValue = Message.Value;
				}
			}

			//------------------------------
			/// <summary>
			/// 最新の状態を取得します。
			/// </summary>
			public override dynamic Value { get => base.Value; set { } }

			#endregion
		}//end of class Validation

		//====================================================================================
		/// <summary>
		/// 論理式を評価してトリガを発生します。
		/// </summary>
		/// <example>
		/// Triggerの使用例を示します。論理式の評価は<see cref="LogicExpression"/>を使用します。
		/// <code>
		/// bool Triggered;//トリガがかかったかどうかを示すフラグ。
		/// 
		/// void TestTrigger() {
		///   string Expression = "TickTimer>5 &amp; TurboRotation=true";//TickTimerが5を越え、かつTurboRotationがtrueである、が実現したらトリガを発生できます。
		///   object[] Owners = { this, Spectrometer, Separator };//論理式に含まれる TickTimer と TurboRotation は、3つのオブジェクトのどれかが親オブジェクトです。
		///   Expression.Trigger Trigger = new Expression.Trigger(Expression, Owners);
		///   
		///   if(Trigger != null) {
		///     Trigger.Subscribe(Ticked);//トリガを処理するメソッドとして、<see cref="EventMessage"/>を受け付ける Ticked メソッドを登録します。
		///     Trigger.IsActive = true;//トリガ動作を開始します。
		///     Triggered = false;//フラグをfalseにします。
		///     while(true) {
		///       System.Threading.Thread.Sleep(100);//100ミリ秒ウェイトします。
		///       if(Trigger.Value | Triggered)//トリガの値がtrueになるか、triggeredがtrueになったら、
		///         break;//ループを抜けます。
		///     }
		///     Trigger.IsActive = false;//Triggerを無効にします。
		///   }
		/// }
		/// 
		/// void Ticked(object Sender, EventMessage Message) {
		///   Triggered = true;//フラグをtrueにします。
		///   Console.WriteLine("\n\n" + $@"TICKED!!!!!! from {Message.Source},{Message.Value}" + "\n\n");
		/// }
		/// </code>
		/// </example>
		public class Trigger : Channel, IHandler {
			#region フィールド

			//------------------------------
			/// <summary>
			/// トリガを生成する論理式を取得します。
			/// </summary>
			public string Expression { get; private set; }

			//------------------------------
			/// <summary>
			/// トリガ生成回数を取得します。0以下なら無限回、1以上ならその回数だけトリガを発生します。
			/// </summary>
			public int Repeat { get; private set; }

			//------------------------------
			/// <summary>
			/// 評価を実施するLogicExpressionオブジェクトを保持します。
			/// </summary>
			Expressions.LogicExpression Evaluator;

			//------------------------------
			/// <summary>
			/// 論理式に含まれる評価式を評価するValidationリストを保持します。
			/// </summary>
			List<Validation> Validators;

			//------------------------------
			/// <summary>
			/// Validationから返された値を保持します。
			/// </summary>
			Dictionary<string, bool> States;//評価結果

			#endregion

			#region メソッド

			/// <summary>
			/// Triggerを初期化します。
			/// </summary>
			/// <param name="Expression">トリガーを発生する条件を記述する論理式を指定します。</param>
			/// <param name="Owner">論理式に含まれるインスタンスの親オブジェクトを指定します。</param>
			/// <param name="Repeat">トリガー発生回数を指定します。</param>
			/// <param name="IsVerbose">詳細モードを指定します。</param>
			public Trigger(object Owner, string Expression, int Repeat = 1, bool IsVerbose = false) : base("Trigger", "Trigger", false, IsVerbose) {
				Validators = new List<Validation>();
				States = new Dictionary<string, bool>();
				LatestValue = false;
				Message.Command = "Triggered";

				if(Owner != null & Expression != "") {//オブジェクトがあって、式が指定されていたら
					this.Expression = Regex.Replace(Expression, @"\s", "");//式を格納し
					this.Repeat = Repeat;//繰り返し数を設定し
					if(IsVerbose)
						Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | トリガ [{this.Expression}] が追加されました。 @Trigger");

					Evaluator = new Expressions.LogicExpression(this.Expression, IsVerbose);//評価インスタンスを生成し
					foreach(var Item in Evaluator.Tokens.Where(v => v.Type == Expressions.TokenType.Logic).Select(v => v.Key)) {//TokenのKeyには比較式が入っている
						Validators.Add(new Validation(Owner, Item, IsVerbose: IsVerbose));//validationリストに追加し
						Validators.Last().Subscribe(HandleMessage);//追加したvalidationの通知先にHandleを登録する
						States.Add(Item, false);//validationの状態バッファを追加
					}
				}
			}

			//------------------------------
			/// <summary>
			/// トリガの動作状態を取得または更新します。
			/// </summary>
			public override bool IsActive {
				get => base.IsActive;
				set {
					if(Validators != null && Validators.Count > 0)
						if(value) {
							base.IsActive = true;
							lock(LockToken)
								for(int Index = Validators.Count - 1; Index >= 0; Index--) {
									Validators[Index].Subscribe(this.HandleMessage);
									Validators[Index].IsActive = true;
								}
							if(base.IsVerbose)
								Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {ChannelName} がアクティベートされました。 @Trigger");
						} else {
							base.IsActive = false;
							lock(LockToken)
								for(int Index = Validators.Count - 1; Index >= 0; Index--) {
									Validators[Index].Unsubscribe(this.HandleMessage);
									Validators[Index].IsActive = false;
								}
							Validators.Clear();//フィールドリストをクリアする
							if(base.IsVerbose)
								Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {ChannelName} がデアクティベートされました。 @Trigger");
						}
				}
			}

			//------------------------------
			/// <summary>
			/// 詳細モードを取得または設定します。
			/// </summary>
			public override bool IsVerbose {
				get => base.IsVerbose;
				set {
					if(value != base.IsVerbose && Validators != null && Validators.Count > 0) {
						foreach(var Item in Validators.Select((v, i) => (v, i)))
							Validators[Item.i].IsVerbose = value;
					}
				}
			}

			//------------------------------
			/// <summary>
			/// Validationオブジェクトからの通知を処理します。
			/// </summary>
			/// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
			/// <param name="Message">EventMessageが格納されています。</param>
			public override void HandleMessage(object Sender, EventMessage Message) {
				if(IsActive) {//アクティベートされていたら
					States[(string)Message.Source] = (bool)Message.Value;
					if(IsVerbose)
						Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | 現在の状態は {string.Join(",", States.Values.Select(v => v.ToString()))} です。 @Trigger");

					bool CurrentState = Evaluator.Evaluate(States);//トリガ評価を行って
					if(CurrentState != base.Value) {//条件が変化したら

						base.Value = CurrentState;//最終値を記録してメッセージを通知する
						if(IsVerbose)
							Console.WriteLine($@"{Message.Stamp.ToString(Formats.Stamp)} | {ChannelName} は {Repeat} 回繰り返します。現在の状態は {string.Join(",", States.Select(v => v.ToString()))} です。 @Trigger");

						Repeat--;//繰り返しカウンタを減らし
						if(Repeat == 0) {//カウンタが0になったら
							this.IsActive = false;//デアクティベートし
						} else if(Repeat < 0)//カウンタが負なら
							Repeat = 0;//カウンタを0にする

					}
				}
			}

			#endregion
		}//end of class Trigger

		//====================================================================================
		/// <summary>
		/// ステップ処理に格納するアクションを保持します。
		/// </summary>
		public class Action {
      #region フィールド

      /// <summary>
      /// Actionの内容を取得します。
      /// </summary>
      public string ActionSource { get; private set; }

      /// <summary>
      /// アクションリストを取得します。
      /// </summary>
      public List<(object Instance, string Value)> Actions { get; private set; }
      #endregion

      #region メソッド

      /// <summary>
      /// Actionを初期化します。
      /// </summary>
      /// <param name="Owner">アクションに含まれるインスタンスの親オブジェクトを指定します。</param>
      /// <param name="ActionSource">アクションを記述する文字列を指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public Action(object Owner, string ActionSource, bool IsVerbose = false) {
        if(ActionSource != "") {//内容があれば
          ActionSource = ActionSource.Replace(" ", "");//空白を消去して
          var Matched = Regex.Matches(ActionSource, Patterns.Action, RegexOptions.IgnoreCase);//記述されたアクション文字列を検索し
          if(Matched.Count > 0) {//見つかったら
            this.ActionSource = ActionSource;//ソースを記録
            this.Actions = new List<(object Instance, string Value)>();//アクションを初期化

            foreach(Match Command in Matched) {//アクション文字列を
              var Items = Command.Value.Split('=');//インスタンスと値に分割し
              var Instance = Utilities.GetInstance(Owner, Items[0], typeof(Channel));//インスタンスを検索し
              if(Instance != null)//インスタンスがあれば
                Actions.Add((Instance, Items.Count() == 2 ? Items[1] : ""));//アクションに追加
            }
          }
          if(IsVerbose)
            Console.WriteLine("\n" + $@"{DateTime.Now.ToString(Formats.Stamp)} | アクション [{ActionSource}] が追加されました。 @Step");
        }
      }

      /// <summary>
      /// このアクションを表す文字列を返します。
      /// </summary>
      /// <returns>アクションを表す文字列を返します。</returns>
      public override string ToString() {
        return $@"{string.Join(",", Actions.Select(v => $@"{((Channel)v.Instance).ChannelName}={v.Value}"))}";
      }
      #endregion
    }//end of class Action

    //====================================================================================
    /// <summary>
    /// シーケンスに格納するステップ処理を保持します
    /// </summary>
    public class Step {
      #region フィールド
      /// <summary>
      /// Actionを取得します。
      /// </summary>
      public Action Action { get; private set; }

      /// <summary>
      /// トリガを取得します。
      /// </summary>
      public Trigger Trigger { get; private set; }

      /// <summary>
      /// Stepの説明を取得します。
      /// </summary>
      public string Description { get; private set; }//このステップの説明
      #endregion

      #region メソッド
      /// <summary>
      /// Stepを初期化します。
      /// </summary>
      /// <param name="Owner">インスタンスの親オブジェクトを指定します。</param>
      /// <param name="Content">ステップ情報を含むXML要素を指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public Step(XmlElement Content, object Owner, bool IsVerbose = false) {
        if(Content != null) {
          this.Action = Content.Attributes["Action"] != null ? new Action(Owner, Content.Attributes["Action"].Value, IsVerbose: IsVerbose) : null;
          this.Trigger = Content.Attributes["Trigger"] != null ? new Trigger(Owner, Content.Attributes["Trigger"].Value, IsVerbose: IsVerbose) : null;
          this.Description = Content.Attributes["Description"] != null ? Content.Attributes["Description"].Value : "";
        }
      }
      #endregion
    }//end of class Step

    //====================================================================================
    /// <summary>
    /// シーケンスを格納します
    /// </summary>
    public class Sequence : Channel {
      #region フィールド
      /// <summary>
      /// 現在のステップを取得します。
      /// </summary>
      public Step CurrentStep { get; private set; }

      /// <summary>
      /// 自動開始フラグを取得します。
      /// </summary>
      public bool AutoStart { get; private set; }

      /// <summary>
      /// 自動終了フラグを取得します。
      /// </summary>
      public bool AutoExit { get; private set; }

      /// <summary>
      /// シーケンスに含まれるステップを保持します。
      /// </summary>
      public Queue<Step> Steps { get; private set; }

      /// <summary>
      /// シーケンスが遷移するときの処理メソッドを保持します。
      /// </summary>
      event EventHandler<Step> OnNext;

      /// <summary>
      /// 処理を一時停止するフラグを保持します。
      /// </summary>
      bool IsPausing;

      //------------------------------
      /// <summary>
      /// Disposeの重複呼び出しを避けるためのフラグを保持します。
      /// </summary>
      bool IsDisposed = false;
      #endregion

      #region メソッド
      /// <summary>
      /// Sequenceを初期化します。
      /// </summary>
      /// <param name="PreferenceFile">スケジュール情報を含むXMLファイル名を指定します。</param>
      /// <param name="Owner">インスタンスの親オブジェクトを指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public Sequence(string PreferenceFile, object Owner, bool IsVerbose = false) : base("Scheduler", "Scheduler", false, IsVerbose) {
        if(PreferenceFile == "")
          PreferenceFile = Debugger.IsAttached ? $@"../../{GetType().Name}.xml" : $@"./{GetType().Name}.xml";
        if(!File.Exists(PreferenceFile)) {
          throw new FileNotFoundException();
        } else {
          XmlDocument XmlDoc = new XmlDocument();
          XmlDoc.Load(PreferenceFile);

          Steps = new Queue<Step>();//Stepsを初期化します。
          var Scheduler = (XmlElement)XmlDoc.SelectSingleNode($@"//Scheduler");
          if(Scheduler != null) {
            var Sequence = Scheduler.Attributes["Active"] != null ? (XmlElement)Scheduler.SelectSingleNode($@"Sequence[@Name=""{Scheduler.Attributes["Active"].Value}""]") : null;
            if(Sequence != null) {
              this.ChannelName = Scheduler.Attributes["Active"].Value;
              this.Message = new EventMessage { Source = $@"Sequence({ChannelName})" };

              foreach(XmlElement Element in Sequence.SelectNodes("Step")) //設定ファイル中のStepを取りだして
                this.Steps.Enqueue(new Step(Element, Owner, IsVerbose));//キューに追加する
              this.OnNext += Invoke;//

              AutoExit = Scheduler.Attributes["AutoExit"] != null ? bool.TryParse(Scheduler.Attributes["AutoExit"].Value, out bool ResultExit) ? ResultExit : false : false;
              IsActive = Scheduler.Attributes["AutoStart"] != null ? bool.TryParse(Scheduler.Attributes["AutoStart"].Value, out bool ResultStart) ? ResultStart : false : false;
            }
          }
        }
      }

      //------------------------------
      /// <summary>
      /// Sequenceを破棄します。
      /// </summary>
      /// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
      protected override void Dispose(bool Disposing) {
        if(!IsDisposed) {
          base.IsActive = false;
          Steps.Clear();

          IsDisposed = true;
          base.Dispose(Disposing);
        }
      }

      /// <summary>
      /// シーケンス処理を開始または停止します。
      /// </summary>
      public override bool IsActive {
        get => base.IsActive;
        set {
          if(value != base.IsActive) {//設定値が変更されるなら
            if(value) {//trueになる場合は
              base.IsActive = true;//アクティベートして
              Invoke(this, Steps.Dequeue());//ステップを実行する
            } else {//falseになる場合は
              base.IsActive = false;//デアクティベートして
              Steps.Clear();//ステップをクリアする
            }
          }
        }
      }

      /// <summary>
      /// シーケンスを中断または再開します。
      /// </summary>
      public bool Pause {
        get => IsPausing;
        set {
          if(value != IsPausing) {//設定値が変更されるなら
            if(value) {//trueになる場合は
              IsPausing = false;//ポーズフラグを下げて
              OnNext(this, Steps.Dequeue());//ステップを再開する
            } else {//falseになる場合は
              IsPausing = true;//ポーズフラグを揚げる
            }
          }
        }
      }

      /// <summary>
      /// ステップ処理を行います。
      /// </summary>
      /// <param name="Sender">イベントを生成したオブジェクトを保持しています。</param>
      /// <param name="CurrentStep">す鉄扉情報を保持しています。</param>
      void Invoke(object Sender, Step CurrentStep) {
        if(CurrentStep != null) {
          System.Threading.Tasks.Task.Run(() => {//別プロセスで実行する
            this.CurrentStep = CurrentStep;

            //ステップの遷移を通知する
            this.Message.Command = "OnNext";
            this.Message.Stamp = DateTime.Now;
            this.Message.Value = this.CurrentStep.Action.ActionSource;
            this.Message.Description = this.CurrentStep.Description;
            Notify(this, this.Message);//base.IsActiveがfalseなら送信は行われない
            if(IsVerbose)
              Console.WriteLine("\n" + $@"{DateTime.Now.ToString(Formats.Stamp)} | ステップを遷移し、{this.CurrentStep.Description}。 @Sequence");


            //アクションを発行する
            if(this.CurrentStep.Action.Actions != null) {
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | " + "\t" + $@"実行されるアクションは {this.CurrentStep.Action.ActionSource} です。");
              if(CurrentStep.Action.Actions.Count > 0)
                foreach(var Action in this.CurrentStep.Action.Actions) {
                  ((Channel)Action.Instance).Value = Action.Value;//base.IsActiveがfalseなら、値は変更されても送信は行われない
                  if(IsVerbose)
                    Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | " + "\t\t" + $@"{Action.Instance}={Action.Value} を実行しました。");
                }
            }

            //条件待ちがあればトリガをセットする
            if(this.CurrentStep.Trigger != null) {
              if(IsVerbose) {
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | " + "\t" + $@"待機するトリガは {this.CurrentStep.Trigger.Expression} です。");
                this.CurrentStep.Trigger.IsVerbose = IsVerbose;
              }
              this.CurrentStep.Trigger.IsActive = true;
              while(!this.CurrentStep.Trigger.Value & IsActive)//Triggerの値がfalseでなく、IsActiveがtrueなら待機
                System.Threading.Thread.Sleep(100);//トリガ待ち
              this.CurrentStep.Trigger.IsActive = false;
            }

            //遷移を判断する
            if(!IsPausing)//ポーズ中でなく
              if(Steps.Count > 0 && IsActive) {//次のステップがあって、IsActiveがtrueなら
                OnNext(this, Steps.Dequeue());//実行する
              } else {//ステップがなければ
                this.Message.Command = "OnCompleted";
                this.Message.Stamp = DateTime.Now;
                this.Message.Value = null;
                Notify(this, this.Message);//シーケンス終了を通知する
              }
          });
        }
      }

      #endregion
    }//end of class Sequence

  }//end of namespace Schedule

}
