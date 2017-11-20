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
using System;

namespace Serilog.Sinks.EventLog
{
    /// <summary>
    /// Hash functions for message templates. See <see cref="Compute(string)"/>.
    /// </summary>
    sealed class EventIdHashProvider : IEventIdProvider
    {
        /// <summary>
        /// Computes an Event Id for the given log event.
        /// </summary>
        /// <param name="logEvent">The log event to compute the event id from.</param>
        /// <returns>Computed event id based off the given log.</returns>
        public ushort ComputeEventId(LogEvent logEvent) => (ushort)Compute(logEvent.MessageTemplate.Text);

        /// <summary>
        /// Compute a 32-bit hash of the provided <paramref name="messageTemplate"/>. The
        /// resulting hash value can be uses as an event id in lieu of transmitting the
        /// full template string.
        /// </summary>
        /// <param name="messageTemplate">A message template.</param>
        /// <returns>A 32-bit hash of the template.</returns>
        static int Compute(string messageTemplate)
        {
            if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

            // Jenkins one-at-a-time https://en.wikipedia.org/wiki/Jenkins_hash_function
            unchecked
            {
                uint hash = 0;
                for (var i = 0; i < messageTemplate.Length; ++i)
                {
                    hash += messageTemplate[i];
                    hash += (hash << 10);
                    hash ^= (hash >> 6);
                }
                hash += (hash << 3);
                hash ^= (hash >> 11);
                hash += (hash << 15);

                //even though the api is type int, eventID must be between 0 and 65535
                //https://msdn.microsoft.com/en-us/library/d3159s0c(v=vs.110).aspx
                return (ushort) hash;
            }
        }
    }
}
