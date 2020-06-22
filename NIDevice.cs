using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using MathNet.Numerics.Statistics;
using NationalInstruments.DAQmx;

namespace RadonLab {

  namespace NIDevice {
    /// <summary>NIデバイスを制御する機能を提供します。APIの詳細は<a href="http://zone.ni.com/reference/en-XX/help/370473J-01/">ここ</a>を参照して下さい。</summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    internal class NamespaceDoc { }

    //*****************************************************************************************************************************
    #region コア
    /// <summary>
    /// NIデバイスを確認するユーティリティーを提供します。
    /// </summary>
    public static class Utilities {

      //====================================================================================
      /// <summary>
      /// デバイス名を取得します。
      /// </summary>
      /// <returns>デバイス名のリストを返します。</returns>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/p_nationalinstruments_daqmx_daqsystem_devices/">ローカルシステム上のDAQシステムインスタンスを取得します。</a>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/p_nationalinstruments_daqmx_daqsystem_devices/">デバイスリストを取得します。</a>
      /// </remarks>
      public static List<string> DeviceNames() {
        return new List<string>(DaqSystem.Local.Devices);
      }

      //====================================================================================
      /// <summary>
      /// デバイスのシリアル番号を取得します。
      /// </summary>
      /// <param name="DeviceName">デバイス名を指定します。</param>
      /// <returns>シリアル番号を返します。</returns>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/m_nationalinstruments_daqmx_daqsystem_loaddevice/">指定名のデバイスを読み込みます。</a>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/p_nationalinstruments_daqmx_device_serialnumber/">シリアル番号を取得します。</a>
      /// </remarks>
      public static long SerialNumber(string DeviceName) {
        return DaqSystem.Local.LoadDevice(DeviceName).SerialNumber;
      }

      //====================================================================================
      /// <summary>
      /// デバイスのモデル名を取得します。
      /// </summary>
      /// <param name="DeviceName">デバイス名を指定します。</param>
      /// <returns>モデル名を返します。</returns>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/m_nationalinstruments_daqmx_daqsystem_loaddevice/">指定名のデバイスを読み込みます。</a>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/p_nationalinstruments_daqmx_device_producttype/">製品タイプを取得します。</a>
      /// </remarks>
      public static string ModelName(string DeviceName) {
        return DaqSystem.Local.LoadDevice(DeviceName).ProductType;
      }

      //====================================================================================
      /// <summary>
      /// 物理ポートを取得します。
      /// </summary>
      /// <param name="DeviceName">デバイス名を指定します。</param>
      /// <param name="Type">物理チャネルのタイプを指定します。</param>
      /// <param name="Access">物理チャネルのアクセスタイプを指定します。</param>
      /// <returns>物理ポートリストを返します。</returns>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/m_nationalinstruments_daqmx_daqsystem_loaddevice/">指定名のデバイスを読み込みます。</a>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/m_nationalinstruments_daqmx_device_getphysicalchannels/">物理チャネルを取得します。</a>
      /// <a href="">製品タイプ</a>
      /// </remarks>
      public static List<string> PhysicalPorts(string DeviceName, PhysicalChannelTypes Type, PhysicalChannelAccess Access = PhysicalChannelAccess.All) {
        return new List<string>(DaqSystem.Local.LoadDevice(DeviceName).GetPhysicalChannels(Type, Access));
      }

      //====================================================================================
      /// <summary>
      /// 物理ポートを取得します。
      /// </summary>
      /// <param name="Type">物理チャネルのタイプを指定します。</param>
      /// <param name="Access">物理チャネルのアクセスタイプを指定します。</param>
      /// <returns>物理ポートリストを返します。</returns>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/p_nationalinstruments_daqmx_daqsystem_devices/">ローカルシステム上のDAQシステムインスタンスを取得します。</a>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/m_nationalinstruments_daqmx_device_getphysicalchannels/">物理チャネルを取得します。</a>
      /// </remarks>
      public static List<string> PhysicalPorts(PhysicalChannelTypes Type, PhysicalChannelAccess Access = PhysicalChannelAccess.All) {
        return new List<string>(DaqSystem.Local.GetPhysicalChannels(Type, Access));
      }

    }//end of static class Utilities
    #endregion

    //*****************************************************************************************************************************
    #region アナログ入力
    //====================================================================================
    /// <summary>
    /// アナログ入力チャネルを定義します。
    /// </summary>
    public class AnalogInputChannel : Channel {
      #region フィールド

      //------------------------------
      /// <summary>
      /// チャネル入力構成を取得します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_aiterminalconfiguration/">AITerminalConfiguration</a>
      /// </remarks>
      public AITerminalConfiguration Configuration { get; private set; }

      //------------------------------
      /// <summary>
      /// 入力最小電圧を取得します。
      /// </summary>
      public double Minimum { get; private set; }

      //------------------------------
      /// <summary>
      /// 入力最大電圧を取得します。
      /// </summary>
      public double Maximum { get; private set; }

      //------------------------------
      /// <summary>
      /// 電圧-物理置変換関数を取得します。
      /// </summary>
      public string Conversion { get; private set; }

      //------------------------------
      /// <summary>
      /// 電圧-物理値変換関数を保持します。
      /// </summary>
      Formula.MathExpression Converter;

      #endregion

      #region メソッド

      //------------------------------
      /// <summary>
      /// アナログ入力チャネルを初期化します。
      /// </summary>
      /// <param name="Name">チャネルの名前を指定します。</param>
      /// <param name="Port">チャネルのポート名をデバイス名から指定します。Dev1/ai0</param>
      /// <param name="Conversion">電圧-物理置変換式を指定します。空の場合は変換しません。</param>
      /// <param name="Minimum">入力電圧の最小値を指定します。</param>
      /// <param name="Maximum">入力電圧の最大値を指定します。</param>
      /// <param name="TerminalConfiguration"><a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_aiterminalconfiguration/">チャネル入力構成</a>を指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public AnalogInputChannel(string Name, string Port, string Conversion = "", double Minimum = -10, double Maximum = 10, AITerminalConfiguration TerminalConfiguration = (AITerminalConfiguration)(-1), bool IsVerbose = false) : base(Name, Port, InitialActivity: false, IsVerbose: IsVerbose) {
        if(Utilities.PhysicalPorts(PhysicalChannelTypes.AI).Exists(v => v.ToLower() == Port.ToLower())) {//指定ポートがシステムに存在していれば
          this.Minimum = Minimum;//最小値
          this.Maximum = Maximum;//最大値
          Configuration = TerminalConfiguration;//チャネル入力構成

          Converter = Conversion != "" ? new Formula.MathExpression(Conversion, IsVerbose) : null;//変換式の解析と変換関数の準備
          this.Conversion = Converter != null ? Conversion : "";//変換関数があれば、変換式を記録
          LatestValue = default(double);//チャネル初期値を実数型に設定
        } else {
          throw new Exception($@"{Port} が見つかりません。 @AnalogInputChannel");
        }
      }

      //------------------------------
      /// <summary>
      /// アナログデータを取得または設定します。
      /// </summary>
      public override dynamic Value {
        get => LatestValue;
        set {
          if(IsActive && value.GetType().Equals(typeof(double))) {//チャネルが有効で、設定値が実数型なら
            value = Converter == null ? value : Converter.Evaluate(value);//変換関数が定義されていたら変換する
            Message.Stamp = DateTime.Now;
            Message.Value = value;
            Notify(this, Message);
            LatestValue = value;
          }
        }
      }

      #endregion
    }//end of class AnalogInputChannel

    //====================================================================================
    /// <summary>
    /// アナログ入力デバイスを定義します。
    /// </summary>
    public class AnalogInputDevice : Device {
      #region フィールド
      //------------------------------
      /// <summary>
      /// アナログデータを一時的に保持します。
      /// </summary>
      double[] Values;//アナログバッファ

      //------------------------------
      /// <summary>
      /// スキャン速度を保持します。
      /// </summary>
      double ScanRate;

      //------------------------------
      /// <summary>
      /// サンプル数を保持します。
      /// </summary>
      int ScanCount;

      //------------------------------
      /// <summary>
      /// トリガエッジを保持します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_sampleclockactiveedge/">SampleClockActiveEdge</a>
      /// </remarks>
      SampleClockActiveEdge TriggerEdge;

      //------------------------------
      /// <summary>
      /// スキャンモードを保持します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_samplequantitymode/">SampleQuantityMode</a>
      /// </remarks>
      SampleQuantityMode ScanMode;

      //------------------------------
      /// <summary>
      /// DAQタスクを保持します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_task/">Task</a>
      /// </remarks>
      NationalInstruments.DAQmx.Task DaqTask;

      //------------------------------
      /// <summary>
      /// アナログリーダを保持します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_analogmultichannelreader/">AnalogMultiChannelReader</a>
      /// </remarks>
      NationalInstruments.DAQmx.AnalogMultiChannelReader Reader;

      //------------------------------
      /// <summary>
      /// 設定完了状態を保持します。
      /// </summary>
      bool IsConfigured;

      object DeviceLockToken;
      #endregion

      #region メソッド

      //------------------------------
      /// <summary>
      /// アナログ入力デバイスを初期化します。
      /// </summary>
      /// <param name="AssignedName">デバイスの識別名を指定しまうs。</param>
      /// <param name="DeviceName">デバイスのモジュール名を指定します。</param>
      /// <param name="DeviceLockToken">デバイスロックトークンを指定します。</param>
      /// <param name="Interval">状態チェックインターバルをミリ秒で指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public AnalogInputDevice(string AssignedName, string DeviceName, object DeviceLockToken = null, double Interval = 250, bool IsVerbose = false) : base(AssignedName, DeviceName, Interval, false, IsVerbose) {
        Values = default;
        DaqTask = new NationalInstruments.DAQmx.Task();
        Reader = new NationalInstruments.DAQmx.AnalogMultiChannelReader(DaqTask.Stream);
        IsConfigured = false;
        ScanRate = 2000;
        ScanCount = 100;
        TriggerEdge = SampleClockActiveEdge.Rising;
        ScanMode = SampleQuantityMode.FiniteSamples;

        this.DeviceLockToken = DeviceLockToken ?? new object();
        if(IsVerbose)
          Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が使用するロックトークンは {this.DeviceLockToken.GetHashCode()} です。 @AnalogInputDevice");
      }

      //------------------------------
      /// <summary>
      /// アナログ入力チャネルを追加します。
      /// </summary>
      /// <param name="NewChannel">アナログ入力チャネルを指定します。</param>
      /// <returns>登録が成功したら登録されたチャネルを返します。登録に失敗した場合はnullを返します。</returns>
      public override Channel Add(Channel NewChannel) {
        if(NewChannel != null & !Channels.Contains(NewChannel)) {
          DaqTask.AIChannels.CreateVoltageChannel(
            ((AnalogInputChannel)NewChannel).ChannelName,
            NewChannel.AssignedName,
            ((AnalogInputChannel)NewChannel).Configuration,
            ((AnalogInputChannel)NewChannel).Minimum,
            ((AnalogInputChannel)NewChannel).Maximum,
            AIVoltageUnits.Volts);
          base.Add(NewChannel);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {NewChannel.AssignedName} が {((AnalogInputChannel)NewChannel).ChannelName} に接続されました。 @AnalogInputDevice");
          return NewChannel;
        } else
          return null;
      }

      //------------------------------
      /// <summary>
      /// オブジェクトを破棄します。
      /// </summary>
      /// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
      protected override void Dispose(bool Disposing) {
        if(!IsDisposed & Disposing) {
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を破棄します。");
          Clock.Stop();
          foreach(var Item in Channels)
            Item.Dispose();
          DaqTask.Dispose();

          IsDisposed = true;
          base.Dispose(Disposing);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を破棄しました。");
        }
      }

      //------------------------------
      /// <summary>
      /// アナログタスクを構成します。チャネルが一つ以上登録されている場合に動作します。
      /// </summary>
      /// <param name="SampleRate">スキャン速度をHzで指定します。</param>
      /// <param name="SamplesPerChannel">取得データ点数を指定します。</param>
      public void Configure(double SampleRate = 2000, int SamplesPerChannel = 100) {
        this.SampleRate = SampleRate;
        this.ScanCount = SamplesPerChannel;
        if(DaqTask.AIChannels.Count > 0) {
          DaqTask.Timing.ConfigureSampleClock("", ScanRate, TriggerEdge, ScanMode, ScanCount);
          DaqTask.Control(TaskAction.Verify);
          IsConfigured = true;
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が構成されました。 @AnalogInputDevice");
        } else {
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} のチャネルは空です。 @AnalogInputDevice");
        }
      }

      //------------------------------
      /// <summary>
      /// デバイスを停止または開始します。
      /// </summary>
      public override bool IsActive {
        get => base.IsActive;
        set {
          if(IsConfigured && value != base.IsActive) {
            if(value) {
              Values = new double[DaqTask.AIChannels.Count];
              Clock.Start();
              base.IsActive = true;
              OnElapsed(this, new EventArgs() as ElapsedEventArgs);
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の計測が開始されました @AnalogInputDevice");
            } else {
              base.IsActive = value;
              Clock.Stop();
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の計測が停止されました。 @AnalogInputDevice");
            }
          }
        }
      }

      //------------------------------
      /// <summary>
      /// タイマイベントごとにスキャンを実施します。
      /// </summary>
      /// <param name="sender">イベントを発生させたオブジェクト情報が格納されています。</param>
      /// <param name="e">イベントパラメータが格納されています。</param>
      protected override void OnElapsed(object sender, ElapsedEventArgs e) {
        NationalInstruments.AnalogWaveform<double>[] Waveforms;
        if(IsActive) {
          try {
            lock(DeviceLockToken)
              Waveforms = Reader.ReadWaveform(-1);//データを取り込む
            if(IsVerbose)
              Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を走査しました。 @AnalogInputDevice");
            foreach(var (Waveform, Index) in Waveforms.Select((v, i) => (v, i)))
              Values[Index] = Waveform.GetRawData().Mean();

            for(int Index = 0; Index < Channels.Count; Index++)
              Channels[Index].Value = Values[Index];
          } catch(Exception Error) {
            Console.WriteLine($"{Error.Message} @AnalogInputDevice.OnElapsed");
          }
        }
      }

      //------------------------------
      /// <summary>
      /// 計測速度を取得または設定します。
      /// </summary>
      public double SampleRate {
        get => ScanRate;
        set {
          if(!IsActive)
            ScanRate = value > 0 ? value : ScanRate;
        }
      }

      //------------------------------
      /// <summary>
      /// チャネルあたりの測定数を取得または設定します。
      /// </summary>
      public int SamplesPerChannel {
        get => ScanCount;
        set {
          if(!IsActive)
            ScanCount = value > 0 ? value : ScanCount;
        }
      }

      //------------------------------
      /// <summary>
      /// スキャントリガの動作開始方向を取得または設定します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_sampleclockactiveedge/">SampleClockActiveEdge</a>
      /// </remarks>
      public SampleClockActiveEdge ActiveEdge {
        get => TriggerEdge;
        set {
          if(!IsActive)
            TriggerEdge = value;
        }
      }

      //------------------------------
      /// <summary>
      /// スキャンモードを取得または設定します。
      /// </summary>
      /// <remarks>
      /// <a href="https://zone.ni.com/reference/en-XX/help/370473J-01/ninetdaqmxfx40ref/html/t_nationalinstruments_daqmx_samplequantitymode/">SampleQuantityMode</a>
      /// </remarks>
      public SampleQuantityMode SampleMode {
        get => ScanMode;
        set {
          if(!IsActive)
            ScanMode = value;
        }
      }

      #endregion
    }//end of class AnalogInputDevice
    #endregion

    //*****************************************************************************************************************************
    #region デジタル出力
    //====================================================================================
    /// <summary>
    /// デジタル出力チャネルを定義します。
    /// </summary>
    public class DigitalOutputChannel : Channel {
      #region フィールド

      //------------------------------
      /// <summary>
      /// チャネル有効評価式を取得します。
      /// </summary>
      public string Validation { get; private set; }

      #endregion

      #region メソッド

      //------------------------------
      /// <summary>
      /// デジタル出力デバイスを初期化します。
      /// </summary>
      /// <param name="Name">チャネルの名前を指定します。</param>
      /// <param name="Port">チャネルのポート名を指定します。</param>
      /// <param name="Validation">チャネルが有効かどうかを評価する式を指定します。空の場合は常に有効です。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public DigitalOutputChannel(string Name, string Port, string Validation = "", bool IsVerbose = false) : base(Name, Port, true, IsVerbose) {
        if(Utilities.PhysicalPorts(PhysicalChannelTypes.DOLine).Exists(v => v.ToLower() == Port.ToLower())) {
          this.Validation = Validation;
          base.Value = false;
        } else {
          throw new Exception($@"{Name} がポート{Port} に見つかりません。 @DigitalOutputChannel");
        }
      }

      //------------------------------
      /// <summary>
      /// デジタル状態を取得または更新します。
      /// </summary>
      public override dynamic Value {
        get => base.Value;
        set {
          if(IsActive) {
            bool Expected = false;
            if(value.GetType().Equals(typeof(bool))) {
              Expected = value;
            } else if(value.GetType().Equals(typeof(string))) {
              Expected = bool.TryParse((string)value, out bool Result) ? Result : false;
            }
            if(IsVerbose)
              Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {ChannelName} の状態は {Expected} です。 @DigitalOutputChannel");
            if(base.Value != Expected)
              base.Value = Expected;
          }
        }
      }

      #endregion
    }//end of class DigitalOutputChannel

    //====================================================================================
    /// <summary>
    /// デジタル出力デバイスを定義します。
    /// </summary>
    public class DigitalOutputDevice : Device {
      #region フィールド

      //------------------------------
      /// <summary>
      /// DAQタスクを保持します。
      /// </summary>
      Dictionary<string, NationalInstruments.DAQmx.Task> DaqTasks;

      //------------------------------
      /// <summary>
      /// デジタルライタを保持します。
      /// </summary>
      Dictionary<string, DigitalSingleChannelWriter> Writers;

      object DeviceLockToken;

      #endregion

      #region メソッド

      //------------------------------
      /// <summary>
      /// デジタル出力デバイスを初期化します。
      /// </summary>
      /// <param name="AssignedName">デバイスの識別名を指定します。</param>
      /// <param name="DeviceName">デバイスのモジュール名を指定します。</param>
      /// <param name="DeviceLockToken">デバイスロックトークンを指定します。</param>
      /// <param name="Interval">状態チェックインターバルをミリ秒で指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public DigitalOutputDevice(string AssignedName, string DeviceName, object DeviceLockToken = null, double Interval = 250, bool IsVerbose = false) : base(AssignedName, DeviceName, Interval, false, IsVerbose) {
        DaqTasks = new Dictionary<string, NationalInstruments.DAQmx.Task>();
        Writers = new Dictionary<string, NationalInstruments.DAQmx.DigitalSingleChannelWriter>();

        this.DeviceLockToken = DeviceLockToken ?? new object();
        if(IsVerbose)
          Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が使用するロックトークンは {this.DeviceLockToken.GetHashCode()} です。 @DigitalOutputDevice");
      }

      //------------------------------
      /// <summary>
      /// デジタル出力チャネルを追加します。
      /// </summary>
      /// <param name="NewChannel">デジタル出力チャネルを指定します。</param>
      /// <returns>登録が成功したら登録されたチャネルを返します。登録に失敗した場合はnullを返します。</returns>
      public override Channel Add(Channel NewChannel) {
        if(NewChannel != null & !Channels.Contains(NewChannel)) {
          DaqTasks.Add(NewChannel.AssignedName, new NationalInstruments.DAQmx.Task());
          DaqTasks[NewChannel.AssignedName].DOChannels.CreateChannel(
            NewChannel.ChannelName,
            NewChannel.AssignedName,
            ChannelLineGrouping.OneChannelForEachLine);

          Writers.Add(NewChannel.AssignedName, new DigitalSingleChannelWriter(DaqTasks[NewChannel.AssignedName].Stream));
          NewChannel.Subscribe(HandleMessage);
          base.Add(NewChannel);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {NewChannel.AssignedName} が {NewChannel.ChannelName} に接続されました。 @DigitalOutputDevice");
          return NewChannel;
        } else
          return null;
      }

      //------------------------------
      /// <summary>
      /// デジタル出力チャネルからの通知を受けて出力を更新します
      /// </summary>
      /// <param name="Sender">メッセージを送信したオブジェクト情報が格納されています。</param>
      /// <param name="Message">送信されたEventMessageが格納されています。</param>
      public override void HandleMessage(object Sender, EventMessage Message) {
        lock(DeviceLockToken)
          Writers[(string)Message.Source].WriteSingleSampleSingleLine(true, (bool)Message.Value);
      }

      //------------------------------
      /// <summary>
      /// デバイスを停止または開始します。
      /// </summary>
      public override bool IsActive {
        get => base.IsActive;
        set {
          if(value != base.IsActive) {
            if(value) {
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の制御を開始しました。 @DigitalOutputDevice");
            } else {
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の制御を停止しました。 @DigitalOutputDevice");
            }
            base.IsActive = value;
          }
        }
      }

      //------------------------------
      /// <summary>
      /// オブジェクトを破棄します。
      /// </summary>
      /// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
      protected override void Dispose(bool Disposing) {
        if(!IsDisposed & Disposing) {
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を破棄します。");
          foreach(var Item in DaqTasks) {
            Writers[Item.Key].WriteSingleSampleSingleLine(true, false);//出力をfalseに戻す
            DaqTasks[Item.Key].Dispose();//タスクをDispose
          }
          Writers.Clear();
          DaqTasks.Clear();

          IsDisposed = true;
          base.Dispose(Disposing);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を破棄しました。");
        }
      }

      #endregion
    }//end of class DigitalOutputDevice
    #endregion

    //*****************************************************************************************************************************
    #region デジタル入力
    //====================================================================================
    /// <summary>
    /// デジタル入力チャネルを定義します。
    /// </summary>
    public class DigitalInputChannel : Channel {
      #region フィールド
      #endregion

      #region メソッド

      //------------------------------
      /// <summary>
      /// デジタル入力チャネルを初期化します。
      /// </summary>
      /// <param name="AssignedName">チャネルの名前を指定します。</param>
      /// <param name="Port">チャネルのポート名を指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public DigitalInputChannel(string AssignedName, string Port, bool IsVerbose = false) : base(AssignedName, Port, InitialActivity: false, IsVerbose: IsVerbose) {
        if(Utilities.PhysicalPorts(PhysicalChannelTypes.DILine).Exists(v => v.ToLower() == Port.ToLower())) {
          LatestValue = false;
        }
      }

      //------------------------------
      /// <summary>
      /// デジタル状態を取得または更新します。
      /// </summary>
      public override dynamic Value {
        get => base.Value;
        set {
          if(IsActive && value.GetType().Equals(typeof(bool))) {
            Message.Stamp = DateTime.Now;
            Message.Value = value;
            Notify(this, Message);
            LatestValue = value;
            if(IsVerbose)
              Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {ChannelName} の状態は {value} です。 @DigitalInputChannel");
          }
        }
      }

      #endregion
    }//end of class DigitalInputChannel

    //====================================================================================
    /// <summary>
    /// デジタル入力デバイスを構成します。
    /// </summary>
    public class DigitalInputDevice : Device {
      #region フィールド

      //------------------------------
      /// <summary>
      /// デジタルデータを一時的に保持します。
      /// </summary>
      public bool[] Values { get; private set; }

      //------------------------------
      /// <summary>
      /// DAQタスクを保持します。
      /// </summary>
      Dictionary<string, NationalInstruments.DAQmx.Task> DaqTasks;

      //------------------------------
      /// <summary>
      /// デジタルリーダを保持します。
      /// </summary>
      Dictionary<string, NationalInstruments.DAQmx.DigitalSingleChannelReader> Readers;

      object DeviceLockToken;

      #endregion

      #region フィールド

      //------------------------------
      /// <summary>
      /// デジタル入力デバイスを初期化します。
      /// </summary>
      /// <param name="AssignedName">デバイスの識別名を指定します。</param>
      /// <param name="DeviceName">デバイスのモジュール名を指定します。</param>
      /// <param name="DeviceLockToken">デバイスロックトークンを指定します。</param>
      /// <param name="Interval">状態チェックインターバルをミリ秒で指定します。</param>
      /// <param name="IsVerbose">詳細モードを指定します。</param>
      public DigitalInputDevice(string AssignedName, string DeviceName, object DeviceLockToken = null, double Interval = 250, bool IsVerbose = false) : base(AssignedName, DeviceName, Interval, false, IsVerbose) {
        DaqTasks = new Dictionary<string, NationalInstruments.DAQmx.Task>();
        Readers = new Dictionary<string, NationalInstruments.DAQmx.DigitalSingleChannelReader>();

        this.DeviceLockToken = DeviceLockToken ?? new object();
        if(IsVerbose)
          Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} が使用するロックトークンは {this.DeviceLockToken.GetHashCode()} です。 @DigitalInputDevice");
      }

      //------------------------------
      /// <summary>
      /// デジタル入力チャネルを追加します。
      /// </summary>
      /// <param name="NewChannel">デジタル入力チャネルを追加します。</param>
      /// <returns>登録が成功したら登録されたチャネルを返します。登録に失敗した場合はnullを返します。</returns>
      public override Channel Add(Channel NewChannel) {
        if(NewChannel != null & !Channels.Contains(NewChannel)) {
          DaqTasks.Add(NewChannel.ChannelName, new NationalInstruments.DAQmx.Task());
          DaqTasks[NewChannel.ChannelName].DIChannels.CreateChannel(
            NewChannel.ChannelName,
            NewChannel.AssignedName,
            ChannelLineGrouping.OneChannelForEachLine);

          Readers.Add(NewChannel.ChannelName, new DigitalSingleChannelReader(DaqTasks[NewChannel.ChannelName].Stream));
          base.Add(NewChannel);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {NewChannel.AssignedName} が {NewChannel.ChannelName} に接続されました。 @DigitalInputChannel");
          return NewChannel;
        } else
          return null;
      }

      //------------------------------
      /// <summary>
      /// デバイスを停止または開始します。
      /// </summary>
      public override bool IsActive {
        get => base.IsActive;
        set {
          if(value != base.IsActive) {
            if(value) {
              Values = new bool[Channels.Count];
              Clock.Start();
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の監視が開始されました。 @DigitalInputChannel");
            } else {
              Clock.Stop();
              if(IsVerbose)
                Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} の監視が停止されました。 @DigitalInputChannel");
            }
            base.IsActive = value;
          }
        }
      }

      //------------------------------
      /// <summary>
      /// タイマイベントごとにスキャンを実施します。
      /// </summary>
      /// <param name="sender">イベントを発生させたオブジェクト情報が格納されています。</param>
      /// <param name="e">イベントパラメータが格納されています。</param>
      protected override void OnElapsed(object sender, ElapsedEventArgs e) {
        if(IsActive) {
          lock(DeviceLockToken)
            foreach(var (Reader, Index) in Readers.Values.Select((v, i) => (v, i)))
              Values[Index] = Reader.ReadSingleSampleSingleLine();
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | {AssignedName} を走査しました。{string.Join(",", Values)} @DigitalInputChannel");

          foreach(var (Item, Index) in Channels.Select((v, i) => (v, i))) //全てのチャネルについて
            ((DigitalInputChannel)Item).Value = Values[Index];
        }
      }

      //------------------------------
      /// <summary>
      /// オブジェクトを破棄します。
      /// </summary>
      /// <param name="Disposing">重複呼び出しを避けるためのフラグを指定します。</param>
      protected override void Dispose(bool Disposing) {
        if(!IsDisposed & Disposing) {
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | DIDevice [{AssignedName}] disposing.");
          Clock.Stop();
          foreach(var DaqTask in DaqTasks.Values)
            DaqTask.Dispose();

          IsDisposed = true;
          base.Dispose(Disposing);
          if(IsVerbose)
            Console.WriteLine($@"{DateTime.Now.ToString(Formats.Stamp)} | DIDevice [{AssignedName}] disposed.");
        }
      }

      #endregion
    }//end of class DigitalInputDevice
    #endregion

  }//end of namespace NIDevice

}