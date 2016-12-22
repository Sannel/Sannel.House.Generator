/* Copyright 2016 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sannel.House.Generator.Generators
{
	public static class ServerSDKStatusConstants
	{
		public static readonly String EnumName = "RequestStatus";
		public static readonly String Unknown = "Unknown";
		public static readonly String ServerUriNotSet = nameof(ServerUriNotSet);
		public static readonly String NotLoggedIn = nameof(NotLoggedIn);
		public static readonly String ServerUriIsNotValid = nameof(ServerUriIsNotValid);
		public static readonly String UnableToConnectToServer = nameof(UnableToConnectToServer);
		public static readonly String Exception = nameof(Exception);
		public static readonly String Error = nameof(Error);
		public static readonly String Success = nameof(Success);
	}
}
