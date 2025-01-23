// Copyright 2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Events;

namespace Serilog.Sinks.EventLog
{
    /// <summary>
    /// Task category number provider for log events
    /// </summary>
    public interface ICategoryNumberProvider
    {
        /// <summary>
        /// Computes an task category number for the given log event.
        /// </summary>
        /// <param name="logEvent">The log event to compute the task category number from.</param>
        /// <returns>Computed task category number based off the given log.</returns>
        short ComputeCategoryNumber(LogEvent logEvent);
    }
}
