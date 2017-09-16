﻿// Accord Statistics Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Statistics.Filters
{
    using System;
    using Accord.Math;
    using MachineLearning;
    using System.Data;
    using Accord.Compat;
    using System.Runtime.Serialization;

    /// <summary>
    ///   Codification Filter class.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The codification filter performs an integer codification of classes in
    ///   given in a string form. An unique integer identifier will be assigned
    ///   for each of the string classes.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <para>
    ///   Every Learn() method in the framework expects the class labels to be contiguous and zero-indexed,
    ///   meaning that if there is a classification problem with n classes, all class labels must be numbers
    ///   ranging from 0 to n-1. However, not every dataset might be in this format and sometimes we will
    ///   have to pre-process the data to be in this format. The example below shows how to use the 
    ///   Codification class to perform such pre-processing.</para>
    ///   
    ///   <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs" region="doc_learn_codification" />
    /// </example>
    /// 
    /// <para>
    ///   Most classifiers in the framework also expect the input data to be of the same nature, i.e. continuous. 
    ///   The codification filter can also be used to convert discrete, categorical, ordinal and baseline categorical
    ///   variables into continuous vectors that can be fed to other machine learning algorithms, such as K-Means:.</para>
    ///   <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\KMeansTest.cs" region="doc_learn_mixed" />
    /// 
    /// <seealso cref="Codification"/>
    /// <seealso cref="Discretization{TInput, TOutput}"/>
    /// 
    [Serializable]
#if NETSTANDARD2_0
    [SurrogateSelector(typeof(Codification.Selector))]
#endif
    public partial class Codification<T> : BaseFilter<Codification<T>.Options, Codification<T>>,
        ITransform<T[], double[]>, IUnsupervisedLearning<Codification<T>, T[], double[]>,
        ITransform<T[], int[]>, IUnsupervisedLearning<Codification<T>, T[], int[]>
    {
        private object defaultMissingValueReplacement = DBNull.Value;

#if !NETSTANDARD1_4
        private bool initialized = false;
#endif

        /// <summary>
        /// Gets the number of outputs generated by the model.
        /// </summary>
        /// 
        /// <value>The number of outputs.</value>
        /// 
        public int NumberOfOutputs
        {
            get
            {
                int total = 0;
                foreach (var col in Columns)
                    total += col.NumberOfSymbols;
                return total;
            }
        }

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification()
        {
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification(DataTable data)
            : this()
        {
            this.Learn(data);
        }

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification(DataTable data, params string[] columns)
            : this()
        {
            for (int i = 0; i < columns.Length; i++)
                Columns.Add(new Options(columns[i], this).Learn(data));
        }
#endif

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification(string columnName, params T[] values)
            : this()
        {
            Columns.Add(new Options(columnName, this).Learn(values));
        }

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification(string[] columnNames, T[][] values)
            : this()
        {
            for (int i = 0; i < columnNames.Length; i++)
                Columns.Add(new Options(columnNames[i], this).Learn(values.GetColumn(i)));
        }

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification(string columnName, T[][] values)
            : this()
        {
            Columns.Add(new Options(columnName, this).Learn(values.Concatenate()));
        }

        /// <summary>
        ///   Gets or sets the default value to be used as a replacement for missing values. 
        ///   Default is to use <c>System.DBNull.Value</c>.
        /// </summary>
        /// 
        public object DefaultMissingValueReplacement
        {
            get { return this.defaultMissingValueReplacement; }
            set { this.defaultMissingValueReplacement = value; }
        }

        int ITransform.NumberOfOutputs
        {
            get { return NumberOfOutputs; }
            set { throw new InvalidOperationException("This property is read only."); }
        }

        /// <summary>
        ///   Translates a value of a given variable
        ///   into its integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="columnName">The name of the variable's data column.</param>
        /// <param name="value">The value to be translated.</param>
        /// 
        /// <returns>An integer which uniquely identifies the given value
        /// for the given variable.</returns>
        /// 
        public int Transform(string columnName, T value)
        {
            return Columns[columnName].Transform(value);
        }

        /// <summary>
        ///   Translates an array of values into their
        ///   integer representation, assuming values
        ///   are given in original order of columns.
        /// </summary>
        /// 
        /// <param name="data">The values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each value
        /// uniquely identifies the given value for each of
        /// the variables.</returns>
        /// 
        public int[] Transform(params T[] data)
        {
            if (this.Columns.Count == 1)
                return this.Columns[0].Transform(data);

            if (data.Length > this.Columns.Count)
            {
                throw new ArgumentException("The array contains more values"
                    + " than the number of known columns.", "data");
            }

            int[] result = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = this.Columns[i].Transform(data[i]);
            return result;
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///   Translates an array of values into their
        ///   integer representation, assuming values
        ///   are given in original order of columns.
        /// </summary>
        /// 
        /// <param name="row">A <see cref="DataRow"/> containing the values to be translated.</param>
        /// <param name="columnNames">The columns of the <paramref name="row"/> containing the
        /// values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each value
        /// uniquely identifies the given value for each of
        /// the variables.</returns>
        /// 
        public int[] Transform(DataRow row, params string[] columnNames)
        {
            var result = new int[columnNames.Length];
            for (int i = 0; i < columnNames.Length; i++)
                result[i] = this.Columns[columnNames[i]].Transform(row);
            return result;
        }
#endif

        /// <summary>
        ///   Translates a value of the given variables
        ///   into their integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="columnNames">The names of the variable's data column.</param>
        /// <param name="values">The values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each integer
        /// uniquely identifies the given value for the given 
        /// variables.</returns>
        /// 
        public int[] Transform(string[] columnNames, T[] values)
        {
            if (columnNames.Length != values.Length)
            {
                throw new ArgumentException("The number of column names"
                    + " and the number of values must match.", "values");
            }

            var result = new int[values.Length];
            for (int i = 0; i < columnNames.Length; i++)
                result[i] = this.Columns[columnNames[i]].Transform(values[i]);
            return result;
        }

        /// <summary>
        ///   Translates a value of the given variables
        ///   into their integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">The values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each integer
        /// uniquely identifies the given value for the given 
        /// variables.</returns>
        /// 
        public int[] Transform(string columnName, T[] values)
        {
            return this.Columns[columnName].Transform(values);
        }

        /// <summary>
        ///   Translates a value of the given variables
        ///   into their integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">The values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each integer
        /// uniquely identifies the given value for the given 
        /// variables.</returns>
        /// 
        public int[][] Transform(string columnName, T[][] values)
        {
            return values.Apply(x => this.Columns[columnName].Transform(x));
        }

        /// <summary>
        ///   Translates a value of the given variables
        ///   into their integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="input">The values to be translated.</param>
        /// 
        /// <returns>An array of integers in which each integer
        /// uniquely identifies the given value for the given 
        /// variables.</returns>
        /// 
        public int[][] Transform(T[][] input)
        {
            var result = new int[input.Length][];
            for (int i = 0; i < input.Length; i++)
                result[i] = Transform(input[i]);
            return result;
        }

        /// <summary>
        ///   Translates a value of the given variables
        ///   into their integer (codeword) representation.
        /// </summary>
        /// 
        /// <param name="input">The values to be translated.</param>
        /// <param name="result">The location to where to store the
        /// result of this transformation.</param>
        /// 
        /// <returns>An array of integers in which each integer
        /// uniquely identifies the given value for the given 
        /// variables.</returns>
        /// 
        public int[][] Transform(T[][] input, int[][] result)
        {
            for (int i = 0; i < input.Length; i++)
                result[i] = Transform(input[i]);
            return result;
        }

        double[] ITransform<T[], double[]>.Transform(T[] input)
        {
            return Transform(new[] { input }, new double[][] { new double[NumberOfOutputs] })[0];
        }

        double[][] ITransform<T[], double[]>.Transform(T[][] input)
        {
            return Transform(input, Jagged.Zeros(input.Length, NumberOfOutputs));
        }

        /// <summary>
        /// Applies the transformation to a set of input vectors,
        /// producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">The input data to which
        /// the transformation should be applied.</param>
        /// <param name="result">The location to where to store the
        /// result of this transformation.</param>
        /// <returns>The output generated by applying this
        /// transformation to the given input.</returns>
        public double[][] Transform(T[][] input, double[][] result)
        {
            for (int j = 0; j < input.Length; j++)
            {
                T[] x = input[j];
                double[] r = result[j];

                int c = 0;
                for (int i = 0; i < Columns.Count; i++)
                {
                    Options col = Columns[i];

                    if (col.VariableType == CodificationVariable.Continuous)
                    {
                        result[j][c++] = (double)System.Convert.ChangeType(x[i], typeof(double));
                    }
                    else if (col.VariableType == CodificationVariable.Discrete)
                    {
                        result[j][c++] = Math.Round((double)System.Convert.ChangeType(x[i], typeof(double)));
                    }
                    else
                    {
                        for (int k = 0; k < col.NumberOfSymbols; k++)
                            r[c + k] = 0;
                        r[c + col.Transform(x[i])] = 1;
                        c += col.NumberOfSymbols;
                    }
                }
            }

            return result;
        }



        /// <summary>
        ///   Translates an integer (codeword) representation of
        ///   the value of a given variable into its original
        ///   value.
        /// </summary>
        /// 
        /// <param name="columnName">The variable name.</param>
        /// <param name="codeword">The codeword to be translated.</param>
        /// 
        /// <returns>The original meaning of the given codeword.</returns>
        /// 
        public T Revert(string columnName, int codeword)
        {
            return this.Columns[columnName].Revert(codeword);
        }

        /// <summary>
        ///   Translates an integer (codeword) representation of
        ///   the value of a given variable into its original
        ///   value.
        /// </summary>
        /// 
        /// <param name="codewords">The codewords to be translated.</param>
        /// 
        /// <returns>The original meaning of the given codeword.</returns>
        /// 
        public T[] Revert(int[] codewords)
        {
            if (this.Columns.Count != 1)
                throw new InvalidOperationException("This method can only be called when there is a single output column.");

            return this.Columns[0].Revert(codewords);
        }

        /// <summary>
        ///   Translates an integer (codeword) representation of
        ///   the value of a given variable into its original
        ///   value.
        /// </summary>
        /// 
        /// <param name="columnName">The name of the variable's data column.</param>
        /// <param name="codewords">The codewords to be translated.</param>
        /// 
        /// <returns>The original meaning of the given codeword.</returns>
        /// 
        public T[] Revert(string columnName, int[] codewords)
        {
            return this.Columns[columnName].Revert(codewords);
        }

        /// <summary>
        ///   Translates the integer (codeword) representations of
        ///   the values of the given variables into their original
        ///   values.
        /// </summary>
        /// 
        /// <param name="columnNames">The name of the variables' columns.</param>
        /// <param name="codewords">The codewords to be translated.</param>
        /// 
        /// <returns>The original meaning of the given codewords.</returns>
        /// 
        public T[] Revert(string[] columnNames, int[] codewords)
        {
            var result = new T[codewords.Length];
            for (int i = 0; i < columnNames.Length; i++)
                result[i] = Revert(columnNames[i], codewords[i]);
            return result;
        }



#if !NETSTANDARD1_4
        /// <summary>
        ///   Processes the current filter.
        /// </summary>
        /// 
        protected override DataTable ProcessFilter(DataTable data)
        {
            // Copy only the schema (Clone)
            DataTable result = data.Clone();

            if (!this.initialized)
                Learn(data);

            // For each column having a mapping
            foreach (Options options in Columns)
            {
                if (!result.Columns.Contains(options.ColumnName))
                    continue;

                // If we are just converting strings to integer codes
                if (options.VariableType == CodificationVariable.Ordinal)
                {
                    // Change its type from string to integer
                    result.Columns[options.ColumnName].MaxLength = -1;
                    if (options.HasMissingValue && options.MissingValueReplacement != null && options.MissingValueReplacement != DBNull.Value)
                    {
                        result.Columns[options.ColumnName].DataType = options.MissingValueReplacement.GetType();
                    }
                    else
                    {
                        result.Columns[options.ColumnName].DataType = typeof(int);
                    }
                }

                // If we want to avoid implying an order relationship between them
                else if (options.VariableType == CodificationVariable.Categorical)
                {
                    // Create extra columns for each possible value
                    for (int i = 0; i < options.NumberOfSymbols; i++)
                    {
                        // Except for the first, that should be the baseline value
                        T symbolName = options.Mapping.Reverse[i];
                        string factorName = getFactorName(options, symbolName);

                        result.Columns.Add(new DataColumn(factorName, typeof(int))
                        {
                            DefaultValue = 0
                        });
                    }

                    // Remove the column from the schema
                    result.Columns.Remove(options.ColumnName);
                }

                // If we want to avoid implying an order relationship between them
                else if (options.VariableType == CodificationVariable.CategoricalWithBaseline)
                {
                    // Create extra columns for each possible value
                    for (int i = 1; i < options.NumberOfSymbols; i++)
                    {
                        // Except for the first, that should be the baseline value
                        T symbolName = options.Mapping.Reverse[i];
                        string factorName = getFactorName(options, symbolName);

                        result.Columns.Add(new DataColumn(factorName, typeof(int))
                        {
                            DefaultValue = 0
                        });
                    }

                    // Remove the column from the schema
                    result.Columns.Remove(options.ColumnName);
                }

                else if (options.VariableType == CodificationVariable.Continuous)
                {
                    // Change its type from to double
                    result.Columns[options.ColumnName].MaxLength = -1;
                    result.Columns[options.ColumnName].DataType = typeof(double);
                }

                else if (options.VariableType == CodificationVariable.Discrete)
                {
                    // Change its type from to int
                    result.Columns[options.ColumnName].MaxLength = -1;
                    result.Columns[options.ColumnName].DataType = typeof(double);
                }

                else
                {
                    throw new InvalidOperationException("Unknown variable type: " + options.VariableType);
                }
            }


            // Now for each row on the original table
            foreach (DataRow inputRow in data.Rows)
            {
                // We'll import to the result table
                DataRow resultRow = result.NewRow();

                // For each column in original table
                foreach (DataColumn column in data.Columns)
                {
                    string name = column.ColumnName;

                    // If the column has a mapping
                    if (Columns.Contains(name))
                    {
                        var options = Columns[name];
                        var map = options.Mapping;
                        var obj = inputRow[name];

                        // Retrieve string value
                        if (options.IsMissingValue(obj))
                        {
                            resultRow[name] = options.MissingValueReplacement;
                            continue;
                        }

                        T label = (T)obj;

                        if (options.VariableType == CodificationVariable.Ordinal)
                        {
                            int value = -1;

                            // Get its corresponding integer
                            try { value = map[label]; }
                            catch
                            {
                                value = map.Values.Count + 1;
                                map[label] = value;
                            }

                            // Set the row to the integer
                            resultRow[name] = value;
                        }
                        else if (options.VariableType == CodificationVariable.CategoricalWithBaseline)
                        {
                            if (options.Mapping[label] > 0)
                            {
                                // Find the corresponding column
                                var factorName = getFactorName(options, label);

                                try
                                {
                                    resultRow[factorName] = 1;
                                }
                                catch { }
                            }
                        }
                        else if (options.VariableType == CodificationVariable.Categorical)
                        {
                            // Find the corresponding column
                            var factorName = getFactorName(options, label);

                            try
                            {
                                resultRow[factorName] = 1;
                            }
                            catch { }
                        }
                        else if (options.VariableType == CodificationVariable.Continuous)
                        {
                            resultRow[name] = obj;
                        }
                        else if (options.VariableType == CodificationVariable.Discrete)
                        {
                            resultRow[name] = obj;
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown variable type: " + options.VariableType);
                        }
                    }
                    else
                    {
                        // The column does not have a mapping
                        //  so we'll just copy the value over
                        resultRow[name] = inputRow[name];
                    }
                }

                // Finally, add the row into the result table
                result.Rows.Add(resultRow);
            }

            return result;
        }
#endif

        private static string getFactorName(Options options, T name)
        {
            return options.ColumnName + ": " + name;
        }



        /// <summary>
        /// Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>A model that has learned how to produce suitable outputs
        /// given the input data <paramref name="x" />.</returns>
        public Codification<T> Learn(T[] x, double[] weights = null)
        {
            if (weights != null)
                throw new ArgumentException(Accord.Properties.Resources.NotSupportedWeights, "weights");

            if (this.Columns.Count == 0)
                this.Columns.Add(new Options("0", this));
            if (this.Columns.Count != 1)
                throw new Exception("There are more predefined columns than columns in the data.");

            Columns[0].Learn(x, weights);
#if !NETSTANDARD1_4
            this.initialized = true;
#endif
            return this;
        }

        /// <summary>
        /// Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>A model that has learned how to produce suitable outputs
        /// given the input data <paramref name="x" />.</returns>
        public Codification<T> Learn(T[][] x, double[] weights = null)
        {
            if (weights != null)
                throw new ArgumentException(Accord.Properties.Resources.NotSupportedWeights, "weights");

            for (int i = this.Columns.Count; i < x.Columns(); i++)
                this.Columns.Add(new Options(i.ToString(), this));
            if (this.Columns.Count != x.Columns())
                throw new Exception("There are more predefined columns than columns in the data.");

            for (int i = 0; i < Columns.Count; i++)
                Columns[i].Learn(x.GetColumn(i), weights);
#if !NETSTANDARD1_4
            this.initialized = true;
#endif
            return this;
        }

#if !NETSTANDARD1_4
        /// <summary>
        /// Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>A model that has learned how to produce suitable outputs
        /// given the input data <paramref name="x" />.</returns>
        public Codification<T> Learn(DataTable x, double[] weights = null)
        {
            if (weights != null)
                throw new ArgumentException(Accord.Properties.Resources.NotSupportedWeights, "weights");

            foreach (DataColumn col in x.Columns)
            {
                if (!this.Columns.Contains(col.ColumnName))
                {
                    if (col.DataType == typeof(T))
                        Columns.Add(new Options(col.ColumnName, this));
                }
            }

            foreach (DataColumn col in x.Columns)
            {
                if (col.DataType == typeof(T))
                    this.Columns[col.ColumnName].Learn(x, weights);
            }

            this.initialized = true;

            return this;
        }
#endif

        /// <summary>
        ///   Converts this instance into a transform that can generate double[].
        /// </summary>
        /// 
        public ITransform<T[], double[]> ToDouble()
        {
            return (ITransform<T[], double[]>)this;
        }

        /// <summary>
        ///   Add a new column options definition with the given variable type to the collection.
        /// </summary>
        /// 
        public void Add(CodificationVariable variableType)
        {
            this.Add(new Options(this.Columns.Count.ToString(), variableType, this));
        }

        [OnDeserializedAttribute]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var option in this)
                option.Owner = this;
        }
    }
}
