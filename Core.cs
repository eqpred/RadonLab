using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Data;
using System.Linq;
using System.Timers;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;

using System.Net.Mail;
using System.IO.Compression;

namespace RadonLab {
	/// <summary>RadonLabシステムのコア機能を提供します。</summary>
	[System.Runtime.CompilerServices.CompilerGenerated]
	internal class NamespaceDoc { }

	//====================================================================================
	/// <summary>
	/// 正規表現で使用するパターンを提供します。
	/// </summary>
	public static class Patterns {

		//------------------------------
		/// <summary>
		/// アクションを記述します。
		/// </summary>
		public const string Action = @"[\w\d\.:]+[=\w\d]*";

		//------------------------------
		/// <summary>
		/// ステップを記述します。
		/// </summary>
		public const string Step = @"(" + Action + @",?)+";

	}

	//====================================================================================
	/// <summary>
	/// ToStringで用いる出力フォーマットパターンを提供します。
	/// </summary>
	public static class Formats {

		//------------------------------
		/// <summary>
		/// タグを記述します。
		/// </summary>
		public const string Tag = "yyyyMMddHHmmss";

		//------------------------------
		/// <summary>
		/// 日時を記述します。
		/// </summary>
		public const string Stamp = "yyyy/MM/dd HH:mm:ss.ff";

	}//end of static class Formats

	//====================================================================================
	/// <summary>
	/// ユーティリティーを提供します。
	/// </summary>
	public static class Utilities {

		//------------------------------
		/// <summary>
		/// オブジェクトをバイト列に展開します。
		/// </summary>
		/// <param name="Object">オブジェクトを指定します。オブジェクトは[Serializable]でなければなりません。</param>
		/// <returns>バイト列に展開されたオブジェクトを返します。</returns>
		public static byte[] Serialize(object Object) {
			if(Object != null) {
				BinaryFormatter Formatter = new BinaryFormatter();
				MemoryStream BinaryStream = new MemoryStream();
				try {
					Formatter.Serialize(BinaryStream, Object);
					return BinaryStream.ToArray();
				} catch(Exception Error) {
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {Error.Source}で、シリアライズエラーが発生しました。エラーコード:{Error.HResult}, メッセージ:{Error.Message}");
					return null;
				}
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// オブジェクトをバイト列から復元します。
		/// </summary>
		/// <param name="Block">展開されたオブジェクトのバイト列を指定します。</param>
		/// <returns>復元されたオブジェクトを返します。</returns>
		public static object Deserialize(byte[] Block) {
			if(Block != null) {
				BinaryFormatter Formatter = new BinaryFormatter();
				MemoryStream BinaryStream = new MemoryStream(Block);
				try {
					return Formatter.Deserialize(BinaryStream);
				} catch(Exception Error) {
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {Error.Source}で、デシリアライズエラーが発生しました。エラーコード:{Error.HResult}, メッセージ:{Error.Message}");
					return default;
				}
			} else
				return default;
		}

		//------------------------------
		/// <summary>
		/// 指定した名前のインスタンスを取得します。
		/// </summary>
		/// <param name="Name">object.methodの形式でインスタンスを指定します。</param>
		/// <param name="PrimaryOwner">objecを含む親オブジェクトを指定します。</param>
		/// <param name="Flags">検索フラグを指定します。</param>
		/// <returns>インスタンスを返します。インスタンスが見つからなければnullを返します。</returns>
		public static object GetInstanceMethod(string Name, object PrimaryOwner, BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy) {
			var Matched = Regex.Match(Name, @"(?<object>\w+)\.(?<method>\w+)");
			if(Matched.Success) {
				var Object = PrimaryOwner.GetType().GetFields(Flags).Where(v => Regex.IsMatch(v.Name.ToLower(), Matched.Groups["object"].Value.ToLower()));
				if(Object.Count() > 0) {
					var Method = Object.First().GetValue(PrimaryOwner).GetType().GetFields(Flags).Where(v => Regex.IsMatch(v.Name.ToLower(), Matched.Groups["method"].Value.ToLower()));
					if(Method.Count() > 0)
						return Method.First().GetValue(Object.First().GetValue(PrimaryOwner));
				}
			}
			return null;
		}

		//------------------------------
		/// <summary>
		/// 指定した名前のインスタンスを取得します。
		/// </summary>
		/// <param name="InstanceName">インスタンス名を指定します。</param>
		/// <param name="Owners">指定したインスタンスの親オブジェクトを指定します。</param>
		/// <param name="Type">インスタンスの型を指定します。</param>
		/// <param name="Flags">検索フラグを指定します。</param>
		/// <returns>インスタンスを返します。インスタンスが見つからなければnullを返します。</returns>
		public static object GetInstance(string InstanceName, object[] Owners, Type Type, BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy) {
			object Instance = null;
			foreach(var Owner in Owners) {
				var Field = Owner.GetType().GetFields(Flags).Where(v => Regex.IsMatch(v.Name.ToLower(), InstanceName.ToLower()) & Type.IsAssignableFrom(v.FieldType));
				if(Field.Count() > 0) {
					Instance = Field.First().GetValue(Owner);
					break;
				}
			}
			return Instance;
		}

		//------------------------------
		/// <summary>
		/// 指定した名前のインスタンスを取得します。
		/// </summary>
		/// <param name="Owner">指定したインスタンスの親オブジェクトを指定します。「this」から開始できます。</param>
		/// <param name="Name">インスタンス名を指定します。「MassSpectrometer.Standam.Ionize」のように記述します。</param>
		/// <param name="ExpectedType">取得したいインスタンスの型を指定します。</param>
		/// <param name="Flags">検索フラグを指定します。</param>
		/// <returns>インスタンスを返します。インスタンスが見つからなければnullを返します。</returns>
		public static object GetInstance(object Owner, string Name, Type ExpectedType = null, BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) {
			var Matched = Regex.Match(Name, @"^(?<parent>\w+)([\.:](?<child>.*))?");
			IEnumerable<FieldInfo> InstanceInfo;
			if(Matched.Groups["child"].Value != "")
				InstanceInfo = Owner.GetType().GetFields(Flags).Where(v => v.Name.ToLower().Contains(Matched.Groups["parent"].Value.ToLower()));
			else
				InstanceInfo = Owner.GetType().GetFields(Flags).Where(v => v.Name.ToLower().Contains(Matched.Groups["parent"].Value.ToLower()) & (ExpectedType != null ? ExpectedType.IsAssignableFrom(v.FieldType) : true));
			if(InstanceInfo.Count() > 0) {
				var Instance = InstanceInfo.First().GetValue(Owner);
				return Matched.Groups["child"].Value == "" ? Instance : GetInstance(Instance, Matched.Groups["child"].Value, ExpectedType);
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// 指定したインスタンスのフィールドの変数型を返します。
		/// </summary>
		/// <param name="Instance">Channelを継承したインスタンスを指定します。</param>
		/// <param name="Name">フィールドの名前を指定します。</param>
		/// <param name="Flags">検索フラグを指定します。</param>
		/// <returns>フィールドの変数型を返します。フィールドが見つからなければnullを返します。</returns>
		public static Type GetParameterType(Channel Instance, string Name, BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) {
			var Field = Instance.GetType().GetFields(Flags).Where(v => Regex.IsMatch(v.Name.ToLower(), Name.ToLower()));
			if(Field.Count() > 0)
				return Field.First().GetValue(Instance).GetType();
			else
				return null;
		}

		//------------------------------
		/// <summary>
		/// 入力文字をT型にキャストできるかどうかを示します。
		/// </summary>
		/// <typeparam name="T">変数型を指定します</typeparam>
		/// <param name="Input">文字列を指定します。</param>
		/// <returns>キャスト可能ならture、キャスト不可能ならfalseを示します。</returns>
		public static bool Is<T>(string Input) {
			try {
				TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(Input);
			} catch(Exception Error) {
				Console.WriteLine($"{Error.Message} @Core.Is<T>");
				return false;
			}
			return true;
		}

		//------------------------------
		/// <summary>
		/// スレッド上限を再設定します。
		/// </summary>
		/// <param name="NumberOfThreads">スレッド上限値を8以上の値で指定します。</param>
		/// <returns>設定されたスレッド上限値を返します。</returns>
		public static int SetWorkerThreads(int NumberOfThreads = 8) {
			ThreadPool.GetMinThreads(out int NumOfWorkerThread, out int NumOfCompletionThread);
			NumberOfThreads = NumberOfThreads > 8 ? NumberOfThreads : NumOfWorkerThread;
			ThreadPool.SetMinThreads(NumberOfThreads, NumOfCompletionThread);
			return NumberOfThreads;
		}

		//------------------------------
		/// <summary>
		/// 指定長さのハッシュバイト列を生成します。
		/// </summary>
		/// <param name="Length">長さを指定します。</param>
		/// <returns>ハッシュバイト列を返します。</returns>
		public static byte[] GetHash(int Length = 64) {
			RandomNumberGenerator RandomGenerator = RandomNumberGenerator.Create();
			byte[] Key = new byte[Length];
			RandomGenerator.GetBytes(Key);
			return new SHA1CryptoServiceProvider().ComputeHash(Key);
		}

		//------------------------------
		/// <summary>
		/// 読みやすくフォーマットされたXML文字列を取得します。
		/// </summary>
		/// <param name="XmlDoc">フォーマット化するXMLドキュメントを指定します。</param>
		/// <param name="IndentChars">インデント文字列を指定します。デフォルトは空白2文字です。</param>
		/// <returns>フォーマット化されたXMLを返します。</returns>
		public static string FormatXmlReadable(XmlDocument XmlDoc, string IndentChars = "  ") {
			var WriterSettings = new System.Xml.XmlWriterSettings() { Encoding = System.Text.Encoding.UTF8 };
			WriterSettings.Indent = true;
			WriterSettings.IndentChars = IndentChars; // <- インデントの空白数ではなくて、1つ分のインデントとして使う文字列を直接指定します。

			using(var WriteBuffer = new System.IO.MemoryStream()) {
				using(var FormatWriter = System.Xml.XmlWriter.Create(WriteBuffer, WriterSettings)) {
					XmlDoc.WriteContentTo(FormatWriter);
					FormatWriter.Flush();
					WriteBuffer.Flush();
				}
				WriteBuffer.Position = 0;
				using(var ReadBuffer = new System.IO.StreamReader(WriteBuffer, System.Text.Encoding.UTF8)) {
					return ReadBuffer.ReadToEnd();
				}
			}
		}

		//------------------------------
		/// <summary>
		/// Xml文字列中のXml宣言を取り除きます。
		/// </summary>
		/// <param name="SourceXml">フォーマット化されたXML文字列を指定します。</param>
		/// <returns>XML宣言が除かれたXML文字列を返します。</returns>
		public static string RemoveXmlDeclaration(string SourceXml = "") {
			if(SourceXml != "")
				return Regex.Replace(SourceXml, "<\\?xml((?!\\?>).)*\\?>\r\n", "");
			else
				return "";
		}

		//------------------------------
		/// <summary>
		/// IPEndPoint形式の文字列からIPEndPointを取得します
		/// </summary>
		/// <param name="EndPointString">EndPoint形式の文字列を指定します。</param>
		/// <returns>EndPoint情報を返します。</returns>
		public static IPEndPoint Parse(string EndPointString) {
			string[] EndPointInfo = EndPointString.Split(':');
			IPAddress Address = IPAddress.TryParse(EndPointInfo[0], out IPAddress ResultAddress) ? ResultAddress : null;
			int Port = int.TryParse(EndPointInfo[1], out int ResultPort) ? ResultPort : 0;
			return new IPEndPoint(Address, Port);
		}

		//------------------------------
		/// <summary>
		/// 指定したファイルをZIPファイルに圧縮します。
		/// </summary>
		/// <param name="ZipName">ZIPファイル名をフルパスで指定します。</param>
		/// <param name="Files">追加するファイルのフルパスのリストを指定します。</param>
		public static void ZipFiles(string ZipName, List<string> Files) {
			using(ZipArchive Archive = ZipFile.Open(ZipName, ZipArchiveMode.Update)) {//これで空zipにファイルを追加できる
				foreach(var NewEntry in Files)
					Archive.CreateEntryFromFile(NewEntry, System.IO.Path.GetFileName(NewEntry));
			}
		}

		//------------------------------
		/// <summary>
		/// 指定したファイルをメールに添付して送信します。
		/// </summary>
		/// <param name="DepartureAccount">送信元メールアカウントを指定します。</param>
		/// <param name="Password">送信元メールアカウントのパスワードを指定します。GMailの場合はアプリパスワードを入力します。</param>
		/// <param name="DestinationAccount">送信先メールアカウントを指定します。</param>
		/// <param name="Subject">メールの題名を指定します。</param>
		/// <param name="Attachments">添付するファイル情報を(ファイル名,MIMEタイプ)のタプルリストで指定します。Textファイルは「text/plain」、XMLファイルは「text/xml」、ZIPファイルは「application/x-zip-compressed」</param>
		public static void SendData(string DepartureAccount, string Password, string DestinationAccount, string Subject, List<(string FileName, string ContentType)> Attachments = null) {
			MailMessage Content = new MailMessage(new MailAddress(DepartureAccount), new MailAddress(DestinationAccount));
			Content.Subject = Subject;
			Content.Body = "";

			foreach(var NewEntry in Attachments)
				Content.Attachments.Add(new Attachment(NewEntry.FileName, new System.Net.Mime.ContentType(NewEntry.ContentType)));// https://webbibouroku.com/Blog/Article/asp-mimetype

			using(SmtpClient MailClient = new SmtpClient("smtp.gmail.com", 587)) {
				MailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
				MailClient.Credentials = new System.Net.NetworkCredential(DepartureAccount, Password);
				MailClient.EnableSsl = true;
				MailClient.Send(Content);
			}

			Content.Dispose();
		}

		//------------------------------
		/// <summary>
		/// 値の仮数と指数を取得します。
		/// </summary>
		/// <param name="Value">値を指定します</param>
		/// <returns>(仮数部,指数部)のタプルを返します。</returns>
		public static (double Mantissa, double Exponent) Decompose(double Value) {
			double digit = Math.Floor(Math.Log10(Value) + 1) - 1;
			double exponent = Math.Pow(10, digit);
			double mantissa = Value / exponent;
			return (mantissa, exponent);
		}


	}//end of static class Utilities

	//====================================================================================
	/// <summary>
	/// イベントメッセージを保持します。
	/// </summary>
	public class EventMessage : EventArgs {
		#region フィールド

		//------------------------------
		/// <summary>
		/// イベントソース情報を取得または設定します。
		/// </summary>
		public object Source { get; set; }

		//------------------------------
		/// <summary>
		/// タイムスタンプを取得または設定します。
		/// </summary>
		public DateTime Stamp { get; set; }

		//------------------------------
		/// <summary>
		/// コマンドを取得または設定します。
		/// </summary>
		public string Command { get; set; }

		//------------------------------
		/// <summary>
		/// 値を取得または設定します。
		/// </summary>
		public object Value { get; set; }

		//------------------------------
		/// <summary>
		/// 説明を取得または設定します。
		/// </summary>
		public string Description { get; set; }

		#endregion

		#region メソッド

		//------------------------------
		/// <summary>
		/// EventMessageを初期化します。
		/// </summary>
		public EventMessage() { }

		//------------------------------
		/// <summary>
		/// メッセージを文字列で取得します。
		/// </summary>
		/// <returns>メッセージを示す文字列を返します。</returns>
		public override string ToString() {
			return $@"Source=""{Source}"", Stamp=""{Stamp.ToString(Formats.Stamp)}"", Command=""{Command}"", Argument=""{(Value != null ? Value.ToString() : "null")}""";
		}

		#endregion
	}//end of class EventMessage

	//================================================================
	/// <summary>
	/// プッシュ型のEventMessage通知を受信するインターフェイスを定義します。
	/// </summary>
	public interface IHandler {

		//------------------------------
		/// <summary>
		/// 名前を取得または設定します。
		/// </summary>
		string AssignedName { get; set; }

		//------------------------------
		/// <summary>
		/// メッセージを受信します。
		/// </summary>
		/// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
		/// <param name="Message">送信されたEventMessageが格納されています。</param>
		void HandleMessage(object Sender, EventMessage Message);

	}//end of interface IHnadler

	//====================================================================================
	/// <summary>
	/// プッシュ型のEventMessage通知を送信するインターフェイスを定義します。
	/// </summary>
	public interface INotifier {

		//------------------------------
		/// <summary>
		/// 名前を取得または設定します。
		/// </summary>
		string AssignedName { get; set; }

		//------------------------------
		/// <summary>
		/// メッセージハンドラを登録します。
		/// </summary>
		/// <param name="NewHandler">登録するイベントハンドラを指定します。</param>
		/// <param name="SendNow">設定値をすぐにUIに送る場合はTrueを指定します。</param>
		/// <returns>登録が成功したら登録されたイベントハンドラを返します。登録に失敗した場合はnullを返します。</returns>
		EventHandler<EventMessage> Subscribe(EventHandler<EventMessage> NewHandler, bool SendNow = false);

		//------------------------------
		/// <summary>
		/// メッセージハンドラを解放します。
		/// </summary>
		/// <param name="Handler">解放するイベントハンドラを指定します。</param>
		/// <returns>解放が成功したら解放されたイベントハンドラを返します。解放に失敗した場合はnullを返します。</returns>
		EventHandler<EventMessage> Unsubscribe(EventHandler<EventMessage> Handler);

		//------------------------------
		/// <summary>
		/// メッセージハンドラを全て解放します。
		/// </summary>
		void ClearHandlers();

		//------------------------------
		/// <summary>
		/// 登録したイベントハンドラにメッセージを送信します。
		/// </summary>
		/// <param name="Sender">イベントを送るインスタンスの情報を指定します。</param>
		/// <param name="Message">送るEventMessageを指定します。</param>
		void Notify(object Sender, EventMessage Message);

	}//end of interface INotifier

	//====================================================================================
	/// <summary>
	/// EventMessageを送受信できるオブジェクトの基底クラスを定義します。
	/// 
	/// イベントの送信はIsActiveがtrueの時に可能になります。
	/// </summary>
	public class Channel : INotifier, IHandler, IDisposable {
		#region フィールド
		/// <summary>
		/// チャネルの物理名を取得または設定します。測定量や制御機器を示す「TubePressure」や「Ionizer」などです。
		/// </summary>
		public string AssignedName { get; set; }

		//------------------------------
		/// <summary>
		/// チャネルの論理名を取得または設定します。デバイスに紐づけられる「ai0」や「port1/line0」などです。
		/// </summary>
		public string ChannelName { get; set; }

		//------------------------------
		/// <summary>
		/// 動作状態を取得または設定します。
		/// </summary>
		public virtual bool IsActive { get; set; }

		//------------------------------
		/// <summary>
		/// 詳細モードを取得または設定します。
		/// </summary>
		public virtual bool IsVerbose { get; set; }

		//------------------------------
		/// <summary>
		/// Disposeの重複呼び出しを避けるためのフラグを保持します。
		/// </summary>
		bool IsDisposed = false;

		//------------------------------
		/// <summary>
		/// イベントハンドラリストを取得します。
		/// </summary>
		public List<EventHandler<EventMessage>> EventHandlers { get; private set; }

		//------------------------------
		/// <summary>
		/// イベントメッセージを保持します。
		/// </summary>
		protected EventMessage Message;

		//------------------------------
		/// <summary>
		/// 最新値を保持します。
		/// </summary>
		protected dynamic LatestValue;

		//------------------------------
		/// <summary>
		/// チャネルロックトークンを保持します・。
		/// </summary>
		public object LockToken { get; set; }

		/// <summary>
		/// 評価を行うValidationインスタンスを保持します。
		/// </summary>
		public Validation Validator;

		#endregion

		#region メソッド

		//------------------------------
		/// <summary>
		/// Channelを初期化します。
		/// </summary>
		/// <param name="AssignedName">チャネルの物理名を指定します。</param>
		/// <param name="ChannelName">チャネルの論理名を指定します。空の場合は物理名が使用されます。</param>
		/// <param name="InitialActivity">コンストラクト時の動作状態を指定します。</param>
		/// <param name="IsVerbose">詳細モードを指定します。</param>
		public Channel(string AssignedName, string ChannelName = "", bool InitialActivity = false, bool IsVerbose = false) {
			this.AssignedName = AssignedName;
			this.ChannelName = ChannelName != "" ? ChannelName : AssignedName;
			this.IsActive = InitialActivity;
			this.IsVerbose = IsVerbose;
			this.LockToken = new object();

			if(IsVerbose)
				Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | チャネル {this.AssignedName} を初期化します。");

			EventHandlers = new List<EventHandler<EventMessage>>();
			Message = new EventMessage() { Source = AssignedName == "" ? GetType().Name : AssignedName, Command = "Update" };
		}

		//------------------------------
		/// <summary>
		/// オブジェクトを破棄します。
		/// </summary>
		/// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
		protected virtual void Dispose(bool Disposing) {
			if(!IsDisposed) {
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | " + "\t" + $@"チャネル {AssignedName} を破棄しています。");
				IsActive = false;

				EventHandlers.Clear();

				IsDisposed = true;
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | " + "\t" + $@"チャネル {AssignedName} を破棄しました。");
			}
		}

		//------------------------------
		/// <summary>
		/// オブジェクトを破棄します。
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}

		//------------------------------
		/// <summary>
		/// メッセージハンドラを登録します。
		/// </summary>
		/// <param name="NewHandler">登録するイベントハンドラを指定します。</param>
		/// <param name="SendNow">設定値をすぐにUIに送る場合はTrueを指定します。</param>
		/// <returns>登録が成功したら登録されたイベントハンドラを返します。登録に失敗した場合はnullを返します。</returns>
		public virtual EventHandler<EventMessage> Subscribe(EventHandler<EventMessage> NewHandler, bool SendNow = false) {
			if(NewHandler != null && !EventHandlers.Contains(NewHandler)) {
				lock(LockToken)
					EventHandlers.Add(NewHandler);
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が {NewHandler.Method.Name} を {NewHandler.GetMethodInfo().Name} に登録しました。 @Channel");

				if(SendNow) {
					Message.Stamp = DateTime.Now;
					Message.Value = LatestValue;
					Notify(this, Message);
				}
				return NewHandler;
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// メッセージハンドラを解放します。
		/// </summary>
		/// <param name="Handler">解放するイベントハンドラ</param>
		/// <returns>解放が成功したら解放されたイベントハンドラを返します。解放に失敗した場合はnullを返します。</returns>
		public virtual EventHandler<EventMessage> Unsubscribe(EventHandler<EventMessage> Handler) {
			if(Handler != null && EventHandlers.Contains(Handler)) {
				lock(LockToken)
					EventHandlers.RemoveAt(EventHandlers.IndexOf(Handler));
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が {Handler.Method.Name} を {Handler.GetMethodInfo().Name} から解放しました。 @Channel");
				return Handler;
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// メッセージハンドラを全て解放します。
		/// </summary>
		public virtual void ClearHandlers() {
			lock(LockToken)
				EventHandlers.Clear();
			if(IsVerbose)
				Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} がすべてのイベントハンドラを解放しました。 @Channel");
		}

		//------------------------------
		/// <summary>
		/// 登録したイベントハンドラにメッセージを送信します。
		/// </summary>
		/// <param name="Sender">イベントを送るインスタンスの情報を指定します。</param>
		/// <param name="Message">送るEventMessageを指定します。</param>
		public virtual void Notify(object Sender, EventMessage Message) {
			lock(LockToken)
				for(int Index = EventHandlers.Count - 1; Index >= 0; Index--) {
					if(IsVerbose)
						Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {Message.Source} が {EventHandlers[Index].Method.Name} に [{Message.ToString()}] を送信しました。 @Channel");
					if(IsActive)
						EventHandlers[Index].Invoke(Sender, Message);
				}
		}

		//------------------------------
		/// <summary>
		/// チャネルデータを取得または更新します。
		/// </summary>
		/// <value>チャネルデータです。</value>
		public virtual dynamic Value {
			get => LatestValue;
			set {
				if(IsActive) {
					Message.Stamp = DateTime.Now;
					Message.Value = value;
					Notify(this, Message);
					LatestValue = value;
				}
			}
		}

		/// <summary>
		/// メッセージを受信します
		/// </summary>
		/// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
		/// <param name="Message">送信されたEventMessageが格納されています。</param>
		public virtual void HandleMessage(object Sender, EventMessage Message) { }

		#endregion
	}//end of class Channel

	//====================================================================================
	/// <summary>
	/// Channelを制御するデバイスオブジェクトの基底クラスを定義します。
	/// </summary>
	public abstract class Device : IHandler, IDisposable {
		#region フィールド
		/// <summary>
		/// デバイスの物理名を取得または設定します。制御機器を示す「Standam」や「AIDevice」などです。
		/// </summary>
		public string AssignedName { get; set; }

		//------------------------------
		/// <summary>
		/// デバイスの論理名を取得または設定します。デバイスを指定する「Dev1」や「COM1」などです。
		/// </summary>
		public string DeviceName { get; set; }

		//------------------------------
		/// <summary>
		/// 動作状態を取得または設定します。
		/// </summary>
		public virtual bool IsActive { get; set; }

		//------------------------------
		/// <summary>
		/// 詳細モードを取得または設定します。
		/// </summary>
		public bool IsVerbose { get; set; }

		//------------------------------
		/// <summary>
		/// Disposeの重複呼び出しを避けるためのフラグを保持します。
		/// </summary>
		protected bool IsDisposed = false;

		//------------------------------
		/// <summary>
		/// インスタンス開始時刻を保持します。
		/// </summary>
		public DateTime OriginTime;

		//------------------------------
		/// <summary>
		/// デバイスクロックを取得します。
		/// </summary>
		public System.Timers.Timer Clock { get; protected set; }

		//------------------------------
		/// <summary>
		/// チャネルリストを取得します。
		/// </summary>
		public List<Channel> Channels { get; protected set; }

		//------------------------------
		/// <summary>
		/// イベントメッセージを保持します。
		/// </summary>
		protected EventMessage Message;

		/// <summary>
		/// ロックトークン
		/// </summary>
		private object ChannelLockToken { get; set; }

		#endregion

		#region メソッド

		/// <summary>
		/// Deviceを初期化します。
		/// </summary>
		/// <param name="AssignedName">デバイスの識別名を指定します。</param>
		/// <param name="DeviceName">モジュール名を指定します。</param>
		/// <param name="Interval">デバイスチェックのインターバル時間をミリ秒で指定します。</param>
		/// <param name="InitialActivity">コンストラクト時の動作状態を指定します。</param>
		/// <param name="IsVerbose">詳細モードを設定します。</param>
		public Device(string AssignedName, string DeviceName = "", double Interval = 250, bool InitialActivity = false, bool IsVerbose = false) {
			this.AssignedName = AssignedName;
			this.DeviceName = DeviceName != "" ? DeviceName : AssignedName;
			this.IsActive = InitialActivity;
			this.IsVerbose = IsVerbose;
			this.ChannelLockToken = new object();

			if(IsVerbose)
				Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | デバイス {AssignedName} を初期化します。");

			Channels = new List<Channel>();
			Message = new EventMessage() { Source = AssignedName == "" ? GetType().Name : AssignedName };

			Clock = new System.Timers.Timer(Interval > 0 ? Interval : 250);
			Clock.Elapsed += OnElapsed;
		}

		//------------------------------
		/// <summary>
		/// チャネルを追加します。
		/// </summary>
		/// <param name="NewChannel">登録するチャネルを指定します。</param>
		/// <returns>登録が成功したら登録されたチャネルを返します。登録に失敗した場合はnullを返します。</returns>
		public virtual Channel Add(Channel NewChannel) {
			if(NewChannel != null & !Channels.Contains(NewChannel)) {
				lock(ChannelLockToken)
					Channels.Add(NewChannel);
				NewChannel.IsActive = true;
				return NewChannel;
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// チャネルを削除します。
		/// </summary>
		/// <param name="Channel">解放するチャネルを指定します。</param>
		/// <returns>解放が成功したら解放されたチャネルを返します。解放に失敗した場合はnullを返します。</returns>
		public virtual Channel Remove(Channel Channel) {
			if(Channel != null & Channels.Contains(Channel)) {
				Channel.IsActive = false;
				lock(ChannelLockToken)
					Channels.RemoveAt(Channels.IndexOf(Channel));
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | チャネル {Channel.ChannelName} を削除しました。");
				return Channel;
			} else
				return null;
		}

		//------------------------------
		/// <summary>
		/// インターバル間隔ごとに実施します。
		/// </summary>
		/// <param name="sender">イベントを発生させたオブジェクト情報が格納されています。</param>
		/// <param name="e">イベントフィールドが格納されています。</param>
		protected virtual void OnElapsed(object sender, ElapsedEventArgs e) { }

		//------------------------------
		/// <summary>
		/// メッセージを受信します。
		/// </summary>
		/// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
		/// <param name="Message">送信されたEventMessageが格納されています。</param>
		public virtual void HandleMessage(object Sender, EventMessage Message) { }

		//------------------------------
		/// <summary>
		/// インデックスを指定して登録されたチャネルを取得します。
		/// </summary>
		/// <param name="Index">チャネルのインデックス</param>
		/// <returns>Channelオブジェクトを返します。</returns>
		public Channel this[int Index] => 0 < Channels.Count & 0 <= Index & Index < Channels.Count ? Channels[Index] : null;

		//------------------------------
		/// <summary>
		/// 名前を指定して登録されたチャネルを取得します。
		/// </summary>
		/// <param name="Name">チャネルの名前</param>
		/// <returns>Channelオブジェクトを返します。</returns>
		public Channel this[string Name] => Channels.FindIndex(v => v.ChannelName == Name) >= 0 ? Channels.First(v => v.ChannelName == Name) : null;

		//------------------------------
		/// <summary>
		/// オブジェクトを破棄します。
		/// </summary>
		/// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
		protected virtual void Dispose(bool Disposing) {
			if(!IsDisposed) {
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | デバイス {AssignedName} を破棄しています。");
				IsActive = false;

				Clock.Stop();
				for(int Index = 0; Index < Channels.Count; Index++)
					Channels[Index].Dispose();
				Channels.Clear();

				IsDisposed = true;
				if(IsVerbose)
					Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | デバイス {AssignedName} を破棄しました。");
			}
		}

		//------------------------------
		/// <summary>
		/// オブジェクトをを破棄します。
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}

		#endregion
	}//end of class Device

	//====================================================================================
	/// <summary>
	/// 時刻を通知するチャネルを定義します。
	/// </summary>
	/// <example>
	/// <see cref="TickDevice"/>から受け取った時刻情報を、<see cref="Channel.Subscribe"/>で登録されたハンドラに通知します。
	/// <code>
	/// TickChannel TickTimer;
	/// TickDevice Clock;
	/// 
	/// void TestTimer() {
	///   Clock = new TickDevice();
	///   TickTimer = (TickChannel)Clock.Add(new TickChannel());
	///   TickTimer.Subscribe(OnElapse);
	///   Clock.IsActive = true;//時間計測を開始します
	/// }
	/// 
	/// void OnElapsed(object Sender, EventMessage Message) {
	///   Console.WriteLine("Elapsed");
	/// }
	/// </code>
	/// </example>
	public class TickChannel : Channel {
		#region フィールド

		//------------------------------
		/// <summary>
		/// 開始時刻を取得または設定します。
		/// </summary>
		public DateTime Origin { get; set; }

		#endregion

		#region メソッド

		//------------------------------
		/// <summary>
		/// TickChannelを初期化します。
		/// </summary>
		/// <param name="IsVerbose">詳細モードを指定します。</param>
		public TickChannel(bool IsVerbose = false) : base("Ticker", IsVerbose: IsVerbose) {
			LatestValue = DateTime.Now;
		}

		//------------------------------
		/// <summary>
		/// 時刻を取得または更新します。
		/// </summary>
		public override dynamic Value {
			get => LatestValue;
			set {
				if(IsActive) {
					Message.Stamp = value;
					Message.Value = value;
					Notify(this, Message);
					LatestValue = value;
					if(IsVerbose)
						Console.WriteLine($@"{Message.Stamp.ToString(Formats.Stamp)} | {AssignedName} の時刻が更新されました。");
				}
			}
		}

		#endregion
	}//end of class TickChannel

	//====================================================================================
	/// <summary>
	/// 時刻を計測するデバイスを定義します。
	/// </summary>
	/// <example>
	/// 時刻を計測して、<see cref="Device.Add"/>で登録された<see cref="TickChannel"/>に時刻をセットします。
	/// <code>
	/// TickChannel TickTimer;
	/// TickDevice Clock;
	/// 
	/// void TestTimer() {
	///   Clock = new TickDevice();
	///   TickTimer = (TickChannel)Clock.Add(new TickChannel());
	///   TickTimer.Subscribe(OnElapse);
	///   Clock.IsActive = true;//時間計測を開始します
	/// }
	/// 
	/// void OnElapsed(object Sender, EventMessage Message) {
	///   Console.WriteLine("Elapsed");
	/// }
	/// </code>
	/// </example>
	public class TickDevice : Device {
		#region フィールド

		//------------------------------
		/// <summary>
		/// 開始時刻を取得または設定します。
		/// </summary>
		public DateTime Origin { get; private set; }

		#endregion

		#region メソッド

		//------------------------------
		/// <summary>
		/// TickDeviceを初期化します。
		/// </summary>
		/// <param name="Interval">時刻チェックのインターバル時間をミリ秒で指定します。</param>
		/// <param name="IsVerbose">詳細モードを指定します。</param>
		public TickDevice(double Interval = 250, bool IsVerbose = false) : base("Clock", Interval: Interval, IsVerbose: IsVerbose) { }

		//------------------------------
		/// <summary>
		/// デバイスを停止または開始します。
		/// </summary>
		public override bool IsActive {
			get => base.IsActive;
			set {
				if(value != base.IsActive) {
					if(value) {
						Origin = DateTime.Now;
						Channels.ForEach(v => { ((TickChannel)v).Origin = Origin; ((TickChannel)v).IsActive = true; });
						Clock.Start();
						if(IsVerbose)
							Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が開始されました。");
					} else {
						Clock.Stop();
						Channels.ForEach(v => ((TickChannel)v).IsActive = false);
						if(IsVerbose)
							Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が停止されました。");
					}
					base.IsActive = value;
				}
			}
		}

		//------------------------------
		/// <summary>
		/// インターバル間隔ごとに時刻をチェックします。
		/// </summary>
		/// <param name="sender">イベントを発生させたオブジェクト情報が格納されています。</param>
		/// <param name="e">イベントフィールドが格納されています。</param>
		protected override void OnElapsed(object sender, System.Timers.ElapsedEventArgs e) {
			for(int Index = Channels.Count - 1; Index >= 0; Index--)
				((TickChannel)Channels[Index]).Value = e.SignalTime;
		}

		#endregion
	}//end of TickDevice

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

}//end of namespace RadonLab
