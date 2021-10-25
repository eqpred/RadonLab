using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RadonLab {

  /// <summary>
  /// パラメータの値を最適化します
  /// <remarks>ここで使用するコードは、NumercalRecipes in C を移植したものです。詳しくは<see href="http://numerical.recipes/"/>をご覧ください。</remarks>
  /// </summary>
  public class Optimization {
    /// <summary>
    /// 詳細表示を行うかどうかを取得または設定します
    /// </summary>
    public bool Verbose { get; set; } = false;
    /// <summary>
    /// 最適化が成功したかどうかを取得します
    /// </summary>
    public bool Succeed { get; private set; } = false;
    /// <summary>
    /// 繰り返し回数を取得します
    /// </summary>
    public int Iteration { get; private set; } = 0;
    /// <summary>
    /// 残差二乗和を取得します
    /// </summary>
    public double ChiSquare { get; private set; } = 0.0;
    /// <summary>
    /// 収束判定のための残差二乗和変化の許容範囲を取得または設定します
    /// </summary>
    public double Tolerance { get; set; } = 0.001;
    /// <summary>
    /// 最適化開始時のダンピング因子を取得または設定します
    /// </summary>
    public double InitialDamper { get; set; } = 0.001;
    /// <summary>
    /// 収束条件が達成されたあとの繰返し回数を取得または設定します
    /// </summary>
    public int ExtraIteration { get; set; } = 4;
    /// <summary>
    /// 最大の繰り返し数を取得または設定します
    /// </summary>
    public int MaximumIteration { get; set; } = 1000;
    /// <summary>
    /// 連立方程式の解法を取得または設定します
    /// </summary>
    public EliminationMethods EliminationMethod { get; set; } = EliminationMethods.Gauss_Jordan;

    /// <summary>
    /// X配列, Y配列, 標準偏差配列を保持します
    /// </summary>
    private double[] X, Y, StandardDeviation;

    //----------------------------------------------------

    /// <summary>
    /// 関数形式を指定します
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Parameters"></param>
    /// <returns></returns>
    public delegate double Function(double X, ParameterCollection Parameters);

    /// <summary>
    /// パラメータを提供します
    /// </summary>
    public class Parameter {
      /// <summary>
      /// パラメータ名を取得または設定します
      /// </summary>
      public string Name { get; set; } = "";
      /// <summary>
      /// パラメータ値を取得または設定します
      /// </summary>
      public double Value { get; set; } = 0.0;
      /// <summary>
      /// 値を固定するかどうかを取得または設定します
      /// </summary>
      public bool Free { get; set; } = true;

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public Parameter() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Name"></param>
      /// <param name="Value"></param>
      /// <param name="Free"></param>
      public Parameter(string Name, double Value, bool Free = true) {
        this.Name = Name;
        this.Value = Value;
        this.Free = Free;
      }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Parameter"></param>
      public Parameter(Parameter Parameter) {
        this.Name = Parameter.Name;
        this.Value = Parameter.Value;
        this.Free = Parameter.Free;
      }

      /// <summary>
      /// パラメータの内容を文字列として取得します
      /// </summary>
      /// <returns></returns>
      public override string ToString() => $"{Name}={Value}({Free})";
    }

    /// <summary>
    /// パラメータのコレクションを提供します
    /// </summary>
    public class ParameterCollection : IList<Parameter> {
      /// <summary>
      /// パラメータのリストを保持します
      /// </summary>
      private List<Parameter> Parameters = new List<Parameter>();
      /// <summary>
      /// 変化可能なパラメータ数を取得します
      /// </summary>
      public int Free { get; private set; } = 0;

      //----------------------------------------------------

      /// <summary>
      /// 初期化します
      /// </summary>
      public ParameterCollection() { }

      /// <summary>
      /// 値を指定して初期化します
      /// </summary>
      /// <param name="Parameters"></param>
      public ParameterCollection(IEnumerable<Parameter> Parameters) {
        foreach(var Entry in Parameters)
          this.Parameters.Add(new Parameter(Entry));
        Free = this.Parameters.Where(Entry => Entry.Free).Count();
      }

      //----------------------------------------------------

      /// <summary>
      /// パラメータ名と値のタプル配列を取得します
      /// </summary>
      public IEnumerable<(string Name, double Value)> NameValues => Parameters.Select(Entry => (Entry.Name, Entry.Value));

      //----------------------------------------------------
      #region コレクション処理
      /// <summary>
      /// 指標のパラメータを取得または設定します
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public Parameter this[int index] {
        get => (0 <= index & index < Parameters.Count) ? Parameters[index] : null;
        set { if(0 <= index & index < Parameters.Count) Parameters[index] = value; }
      }
      /// <summary>
      /// 指定名のパラメータを取得または設定します
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      public Parameter this[string name] {
        get {
          var Index = Parameters.FindIndex(Entry => Entry.Name.Equals(name));
          if(Index != -1)
            return Parameters[Index];
          else
            return null;
        }
        set {
          var Index = Parameters.FindIndex(Entry => Entry.Name.Equals(name));
          if(Index != -1)
            Parameters[Index] = value;
        }
      }
      public int Count => Parameters.Count;
      public bool IsReadOnly => ((ICollection<Parameter>)Parameters).IsReadOnly;
      public void Add(Parameter item) {
        Parameters.Add(new Parameter(item));
        Free = Parameters.Where(Entry => Entry.Free).Count();
      }
      public void Clear() {
        Parameters.Clear();
        Free = 0;
      }
      public bool Contains(Parameter item) => Parameters.Contains(item);
      public void CopyTo(Parameter[] array, int arrayIndex) => Parameters.CopyTo(array, arrayIndex);
      public int IndexOf(Parameter item) => Parameters.IndexOf(item);
      public void Insert(int index, Parameter item) {
        Parameters.Insert(index, item);
        Free = Parameters.Where(Entry => Entry.Free).Count();
      }
      public bool Remove(Parameter item) {
        var Removed = Parameters.Remove(item);
        if(Removed)
          Free = Parameters.Where(Entry => Entry.Free).Count();
        return Removed;
      }
      public void RemoveAt(int index) {
        Parameters.RemoveAt(index);
        Free = Parameters.Where(Entry => Entry.Free).Count();
      }
      public IEnumerator<Parameter> GetEnumerator() => ((IEnumerable<Parameter>)Parameters).GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Parameters).GetEnumerator();
      #endregion
    }

    /// <summary>
    /// 連立方程式の解法を定義します
    /// </summary>
    public enum EliminationMethods {
      Gauss_Jordan,
      LU_Decomposition,
      SV_Decomposition,
      QR_Decomposition,
    }

    //----------------------------------------------------

    /// <summary>
    /// 初期化します
    /// </summary>
    public Optimization() { }

    /// <summary>
    /// データを指定して初期化します
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <param name="StandardDeviation"></param>
    /// <param name="IsVerbose"></param>
    public Optimization(IEnumerable<double> X, IEnumerable<double> Y, double StandardDeviation, bool IsVerbose = false) {
      Bind(X, Y, StandardDeviation);
      Verbose = IsVerbose;
    }

    /// <summary>
    /// データを指定して初期化します
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <param name="StandardDeviation"></param>
    /// <param name="IsVerbose"></param>
    public Optimization(IEnumerable<double> X, IEnumerable<double> Y, IEnumerable<double> StandardDeviation, bool IsVerbose = false) {
      Bind(X, Y, StandardDeviation);
      Verbose = IsVerbose;
    }

    //----------------------------------------------------

    /// <summary>
    /// データをバインドします
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <param name="StandardDeviation"></param>
    public void Bind(IEnumerable<double> X, IEnumerable<double> Y, double StandardDeviation) {
      var NumOfData = Math.Min(X.Count(), Y.Count());
      this.X = X.Take(NumOfData).ToArray();
      this.Y = Y.Take(NumOfData).ToArray();
      this.StandardDeviation = Enumerable.Repeat(StandardDeviation, NumOfData).ToArray();
    }

    /// <summary>
    /// データをバインドします
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <param name="StandardDeviation"></param>
    public void Bind(IEnumerable<double> X, IEnumerable<double> Y, IEnumerable<double> StandardDeviation) {
      var NumOfData = Math.Min(Math.Min(X.Count(), Y.Count()), StandardDeviation.Count());
      this.X = X.Take(NumOfData).ToArray();
      this.Y = Y.Take(NumOfData).ToArray();
      this.StandardDeviation = StandardDeviation.Take(NumOfData).ToArray();
    }

    //----------------------------------------------------

    /// <summary>
    /// 最適化を行います
    /// </summary>
    /// <param name="TrialFunction">試行関数</param>
    /// <param name="Partials">パラメータごとの偏微分関数</param>
    /// <param name="Parameters">初期値を格納したパラメータコレクション</param>
    public void Optimize(Function TrialFunction, IEnumerable<Function> Partials, ref ParameterCollection Parameters) {
      int ExtraIteration = 0;
      double Damper = InitialDamper;
      double LastChiSquare = 0.0;

      double[] Perturbation = new double[Parameters.Count];
      double[] PerturbationBuffer = new double[Parameters.Count];
      double[,] Covariance = new double[Parameters.Count, Parameters.Count];
      double[,] Curvature = new double[Parameters.Count, Parameters.Count];
      double[,] CovarianceMatrix = new double[Parameters.Free, Parameters.Free];
      double[,] PerturbationMatrix = new double[Parameters.Free, 1];
      ParameterCollection TrialParameters = new ParameterCollection(Parameters);

      ChiSquare = Perturb(TrialFunction, Partials, Parameters, ref Curvature, ref PerturbationBuffer);
      if(Verbose)
        Console.WriteLine($"\tChiSquare = {ChiSquare}");
      LastChiSquare = ChiSquare;
      Succeed = false;
      for(Iteration = 0; Iteration < MaximumIteration; Iteration++) {
        if(Verbose)
          Console.WriteLine($"\nIteration = {Iteration}, Extra = {ExtraIteration}");

        if(ExtraIteration == this.ExtraIteration)
          Damper = 0.0;
        for(int Index = 0; Index < Parameters.Free; Index++) {
          for(int Jndex = 0; Jndex < Parameters.Free; Jndex++)
            Covariance[Index, Jndex] = Curvature[Index, Jndex];
          Covariance[Index, Index] = Curvature[Index, Index] * (1.0 + Damper);
          for(int Jndex = 0; Jndex < Parameters.Free; Jndex++)
            CovarianceMatrix[Index, Jndex] = Covariance[Index, Jndex];
          PerturbationMatrix[Index, 0] = PerturbationBuffer[Index];
        }

        try {
          switch(EliminationMethod) {
            default:
              GaussJordan(ref CovarianceMatrix, ref PerturbationMatrix);
              break;
          }
        } catch(Exception Error) {
          throw Error;
        }

        for(int Index = 0; Index < Parameters.Free; Index++) {
          for(int Jndex = 0; Jndex < Parameters.Free; Jndex++)
            Covariance[Index, Jndex] = CovarianceMatrix[Index, Jndex];
          Perturbation[Index] = PerturbationMatrix[Index, 0];
        }
        if(Verbose)
          Console.WriteLine($"\tPerturbation = {string.Join(",", Perturbation)}");
        if(ExtraIteration == this.ExtraIteration) {
          Sort(Parameters, ref Covariance);
          Sort(Parameters, ref Curvature);
          Succeed = true;
          break;
        }
        for(int Index = 0, Kndex = 0; Kndex < Parameters.Count; Kndex++)
          if(Parameters[Kndex].Free)
            TrialParameters[Kndex].Value = Parameters[Kndex].Value + Perturbation[Index++];
        if(Verbose) {
          Console.WriteLine($"\tParameters = {string.Join(",", Parameters.Select(Entry => Entry.Value))}");
          Console.WriteLine($"\tPurtubated = {string.Join(",", TrialParameters.Select(Entry => Entry.Value))}");
        }
        ChiSquare = Perturb(TrialFunction, Partials, TrialParameters, ref Covariance, ref Perturbation);
        if(Verbose)
          Console.WriteLine($"\tChiSquare = {ChiSquare}");
        if(Math.Abs(ChiSquare - LastChiSquare) < Math.Max(Tolerance, Tolerance * ChiSquare))
          ExtraIteration++;
        if(ChiSquare < LastChiSquare) {
          Damper *= 0.1;
          LastChiSquare = ChiSquare;
          for(int Index = 0; Index < Parameters.Free; Index++) {
            for(int Jndex = 0; Jndex < Parameters.Free; Jndex++)
              Curvature[Index, Jndex] = Covariance[Index, Jndex];
            PerturbationBuffer[Index] = Perturbation[Index];
          }
          for(int Kndex = 0; Kndex < Parameters.Count; Kndex++)
            Parameters[Kndex].Value = TrialParameters[Kndex].Value;
        } else {
          Damper *= 10.0;
          ChiSquare = LastChiSquare;
        }
      }
    }

    /// <summary>
    /// パラメータの変動量を計算します
    /// </summary>
    /// <param name="TrialFunction"></param>
    /// <param name="Partials"></param>
    /// <param name="Parameters"></param>
    /// <param name="Curvature"></param>
    /// <param name="Perturbation"></param>
    /// <returns></returns>
    private double Perturb(Function TrialFunction, IEnumerable<Function> Partials, ParameterCollection Parameters, ref double[,] Curvature, ref double[] Perturbation) {
      int i, j, k, l, m;
      double Y, dY, Weight, VarianceInversed;
      double[] Derivatives = new double[Parameters.Count];

      for(j = 0; j < Parameters.Free; j++) {
        for(k = 0; k <= j; k++)
          Curvature[j, k] = 0.0;
        Perturbation[j] = 0.0;
      }

      var ChiSquare = 0.0;
      for(i = 0; i < X.Count(); i++) {
        Y = TrialFunction(X[i], Parameters);
        VarianceInversed = 1.0 / (StandardDeviation[i] * StandardDeviation[i]);
        dY = this.Y[i] - Y;
        for(j = 0; j < Parameters.Count; j++)
          Derivatives[j] = Partials.ElementAt(j)(X[i], Parameters);
        for(j = 0, l = 0; l < Parameters.Count; l++) {
          if(Parameters[l].Free) {
            Weight = Derivatives[l] * VarianceInversed;
            for(k = 0, m = 0; m < l + 1; m++)
              if(Parameters[m].Free)
                Curvature[j, k++] += Weight * Derivatives[m];
            Perturbation[j++] += dY * Weight;
          }
        }
        ChiSquare += dY * dY * VarianceInversed;
      }
      for(j = 1; j < Parameters.Free; j++)
        for(k = 0; k < j; k++)
          Curvature[k, j] = Curvature[j, k];
      return ChiSquare;
    }

    /// <summary>
    /// 行列をソートします
    /// </summary>
    /// <param name="Parameters"></param>
    /// <param name="Covariance"></param>
    private void Sort(ParameterCollection Parameters, ref double[,] Covariance) {
      int i, j, k;
      for(i = Parameters.Free; i < Parameters.Count; i++)
        for(j = 0; j < i + 1; j++)
          Covariance[i, j] = Covariance[j, i] = 0.0;
      k = Parameters.Free - 1;
      for(j = Parameters.Count - 1; j >= 0; j--) {
        if(Parameters[j].Free) {
          for(i = 0; i < Parameters.Count; i++)
            Swap(ref Covariance, i, k, i, j);
          for(i = 0; i < Parameters.Count; i++)
            Swap(ref Covariance, k, i, j, i);
          k--;
        }
      }
    }

    /// <summary>
    /// 行列の要素を交換します
    /// </summary>
    /// <param name="Matrix"></param>
    /// <param name="LeftRow"></param>
    /// <param name="LeftColumn"></param>
    /// <param name="RightRow"></param>
    /// <param name="RightColumn"></param>
    private void Swap(ref double[,] Matrix, int LeftRow, int LeftColumn, int RightRow, int RightColumn) {
      double Buffer = Matrix[RightRow, RightColumn];
      Matrix[LeftRow, LeftColumn] = Matrix[RightRow, RightColumn];
      Matrix[LeftRow, LeftColumn] = Buffer;
    }

    //----------------------------------------------------

    /// <summary>
    /// GaussJordanの消去法で連立方程式の解を計算します
    /// </summary>
    /// <param name="Curvature"></param>
    /// <param name="Perturbation"></param>
    private void GaussJordan(ref double[,] Curvature, ref double[,] Perturbation) {
      int i, j, k, l, ll, Column = 0, Row = 0, NumOfRow = Curvature.GetLength(0), NumOfColumn = Perturbation.GetLength(1);
      double CurvatureMaximum, CurvatureInverse, CurvatureBuffer;
      int[] ColumnIndex = new int[NumOfRow];
      int[] RowIndex = new int[NumOfRow];
      int[] PivotIndex = new int[NumOfRow];

      for(i = 0; i < NumOfRow; i++) {
        CurvatureMaximum = 0.0;
        for(j = 0; j < NumOfRow; j++)
          if(PivotIndex[j] != 1)
            for(k = 0; k < NumOfRow; k++) {
              if(PivotIndex[k] == 0) {
                if(CurvatureMaximum <= Math.Abs(Curvature[j, k])) {
                  CurvatureMaximum = Math.Abs(Curvature[j, k]);
                  Row = j;
                  Column = k;
                }
              } else if(1 < PivotIndex[k]) {
                throw new Exception("gaussj: Singular Matrix-1");
              }
            }
        ++(PivotIndex[Column]);
        if(Row != Column) {
          for(l = 0; l < NumOfRow; l++)
            Swap(ref Curvature, Row, l, Column, l);
          for(l = 0; l < NumOfColumn; l++)
            Swap(ref Perturbation, Row, l, Column, l);
        }
        RowIndex[i] = Row;
        ColumnIndex[i] = Column;
        if(Curvature[Column, Column] == 0.0)
          throw new Exception("gaussj: Singular Matrix-2");
        CurvatureInverse = 1.0 / Curvature[Column, Column];
        Curvature[Column, Column] = 1.0;
        for(l = 0; l < NumOfRow; l++)
          Curvature[Column, l] *= CurvatureInverse;
        for(l = 0; l < NumOfColumn; l++)
          Perturbation[Column, l] *= CurvatureInverse;
        for(ll = 0; ll < NumOfRow; ll++)
          if(ll != Column) {
            CurvatureBuffer = Curvature[ll, Column];
            Curvature[ll, Column] = 0.0;
            for(l = 0; l < NumOfRow; l++)
              Curvature[ll, l] -= Curvature[Column, l] * CurvatureBuffer;
            for(l = 0; l < NumOfColumn; l++)
              Perturbation[ll, l] -= Perturbation[Column, l] * CurvatureBuffer;
          }
      }
      for(l = NumOfRow - 1; l >= 0; l--) {
        if(RowIndex[l] != ColumnIndex[l])
          for(k = 0; k < NumOfRow; k++)
            Swap(ref Curvature, k, RowIndex[l], k, ColumnIndex[l]);
      }
    }

    //----------------------------------------------------

    /// <summary>
    /// 最適化をデモンストレーションします
    /// </summary>
    public static void Demonstrate() {
      double DecayCurve(double x, ParameterCollection p) => p["a"].Value * Math.Exp(-x / p["b"].Value) + p["c"].Value;
      double dfda(double x, ParameterCollection p) => Math.Exp(-x / p["b"].Value);
      double dfdb(double x, ParameterCollection p) => p["a"].Value * x / Math.Pow(p["b"].Value, 2) * Math.Exp(-x / p["b"].Value);
      double dfdc(double x, ParameterCollection p) => 1.0;

      var Generator = new Random((int)DateTime.Now.ToOADate());
      Function Trial = DecayCurve;
      List<Function> Derivatives = new List<Function> { dfda, dfdb, dfdc };

      ParameterCollection Parameters = new ParameterCollection { new Parameter("a", 3.21), new Parameter("b", 1.95), new Parameter("c", 0.53) };
      List<double> X = Enumerable.Range(0, 50).Select(x => x / 10.0).ToList();
      List<double> Y = X.Select(x => Trial(x, Parameters) + (Generator.NextDouble() - 0.5) / 10.0).ToList();
      var Optimizer = new Optimization(X, Y, 0.01);

      ParameterCollection Tentative = new ParameterCollection(Parameters);
      Tentative[0].Value = Parameters[0].Value * (1 + (Generator.NextDouble() - 0.5));
      Tentative[1].Value = Parameters[1].Value * (1 + (Generator.NextDouble() - 0.5));
      Tentative[2].Value = Parameters[2].Value * (1 + (Generator.NextDouble() - 0.5));

      Console.WriteLine($"Best Fit  = {string.Join(",", Parameters.Select(Entry => Entry.Value))}");
      Console.WriteLine($"Initial   = {string.Join(",", Tentative.Select(Entry => Entry.Value))}");
      try {
        Optimizer.Optimize(Trial, Derivatives, ref Tentative);
        Console.WriteLine($"Optimized = {string.Join(",", Tentative.Select(Entry => Entry.Value))}");
        Console.WriteLine($"Iteration = {Optimizer.Iteration}, ChiSq = {Optimizer.ChiSquare}, Succeed = {Optimizer.Succeed}");
      } catch(Exception Error) {
        Console.WriteLine($"\t{Error.Message}");
      }
    }

  }

}
