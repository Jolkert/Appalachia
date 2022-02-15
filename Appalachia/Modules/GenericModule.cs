using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Appalachia.Utility;

namespace Appalachia.Modules
{
	[Group, Name(Source)]
	public class GenericModule : ModuleBase<SocketCommandContext>, IModuleBase
	{
		// should this module even exist? i got rid of it in DexBot at some point. and like. everything *should* belong to another module anyways.
		// if theres nothing here when im finally ready to launch 2.0 i'll get rid of it. $test is at least mildly helpful for now tho -jolk 2022-01-09
		// actually im the dumbest ass. why don't i just move $test into AdminModule? lmao -jolk 2022-02-14
		private const string Source = "Generic";

		[Command("test"), RequireOwner]
		public async Task Test([Remainder]string str = "")
		{
			
		}
	}
}
