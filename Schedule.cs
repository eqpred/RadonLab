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
