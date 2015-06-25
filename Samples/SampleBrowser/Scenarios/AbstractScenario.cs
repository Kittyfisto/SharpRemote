using System.Threading.Tasks;
using System.Windows.Input;

namespace SampleBrowser.Scenarios
{
	public abstract class AbstractScenario
		: IScenario
	{
		private readonly string _name;
		private readonly string _description;
		private readonly ICommand _startCommand;
		private readonly ICommand _stopCommand;

		protected AbstractScenario(string name, string description)
		{
			_name = name;
			_description = description;

			_startCommand = new DelegateCommand(Start);
			_stopCommand = new DelegateCommand(Stop);
		}

		private void Start(object unused)
		{
			Start();
		}

		private void Stop(object unused)
		{
			Stop();
		}

		protected abstract Task Start();
		protected abstract Task Stop();

		public string Name
		{
			get { return _name; }
		}

		public string Description
		{
			get { return _description; }
		}

		public ICommand StartCommand
		{
			get { return _startCommand; }
		}

		public ICommand StopCommand
		{
			get { return _stopCommand; }
		}
	}
}