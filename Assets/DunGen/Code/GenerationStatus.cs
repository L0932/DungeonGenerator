using System;

namespace DunGen
{
	public delegate void GenerationStatusDelegate(DungeonGenerator generator, GenerationStatus status);
	
	public enum GenerationStatus
	{
		NotStarted = 0,
		PreProcessing,
		MainPath,
		Branching,
		PostProcessing,
		Complete,
		Failed,
	}
}
