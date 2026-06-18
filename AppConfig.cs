using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DTE10T_WPF
{
    public class AppConfig
    {
        public List<AlarmModel> AlarmList { get; set; } = new();

        public List<CommParamModel> CommList { get; set; } = new();

        public List<CTModel> CTList { get; set; } = new();

        public List<EventModel> EventList { get; set; } = new();

        public List<HotRunnerModel> HotRunnerList { get; set; } = new();

        public List<InputAdjModel> InputAdjList { get; set; } = new();

        public List<OutputModel> OutputList { get; set; } = new();

        public List<ProgramPatternModel> PatternList { get; set; } = new();

        public List<PIDModel> PIDList { get; set; } = new();

        public List<PVSVModel> PVSVList { get; set; } = new();

        public SerialPortSettings SerialPortSettings { get; set; } = new();

        public List<SlopeModel> SlopeList { get; set; } = new();

        public List<ProgramStepModel> StepList { get; set; } = new();
    }
}
