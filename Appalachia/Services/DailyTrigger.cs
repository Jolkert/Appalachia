using System;
using System.Threading.Tasks;

namespace Appalachia.Services
{
	public class DailyTrigger
	{// this works for now for a daily trigger operaion -jolk 2022-05-01
		public TimeSpan TriggerTime { get; }

		public DailyTrigger(int hour = 0, int minute = 0, int second = 0)
		{
			TriggerTime = new TimeSpan(hour, minute, second);

			Task.Run(async () =>
			{
				while (true)
				{
					DateTime triggerTime = DateTime.Today + TriggerTime;
					if (triggerTime < DateTime.Now)
						triggerTime += new TimeSpan(24, 0, 0);

					await Task.Delay((int)(triggerTime - DateTime.Now).TotalMilliseconds);
					Trigger?.Invoke();
				}
			});
		}

		public event Action Trigger;
	}
}
