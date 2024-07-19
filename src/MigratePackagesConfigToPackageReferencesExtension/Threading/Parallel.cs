// <copyright file="Parallel.cs" company="Rami Abughazaleh">
//   Copyright (c) Rami Abughazaleh. All rights reserved.
// </copyright>

namespace MigratePackagesConfigToPackageReferencesExtension.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Provides support for parallel loops.
    /// </summary>
    internal static class Parallel
    {
        /// <summary>
        /// Executes a foreach operation in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">A data source collection.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent tasks enabled.</param>
        /// <param name="action">The delegate that is invoked once per iteration.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static Task ForEachAsync<TSource>(
            ICollection<TSource> source, int maxDegreeOfParallelism, Func<TSource, Task> action)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            var block = new ActionBlock<TSource>(action, options);

            foreach (var item in source)
            {
                block.Post(item);
            }

            block.Complete();

            return block.Completion;
        }
    }
}
