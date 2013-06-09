FSC=fsharpc
FSC_FLAGS = --optimize
REFS = -r System.dll -r System.Core.dll -r System.Numerics.dll
LIB = $(HOME)/usr/lib/mono/4.5/
STANDALONE = --standalone
SOURCES_DIR = src
COMMON_DIR = $(SOURCES_DIR)/Common
AGENT_DIR = $(SOURCES_DIR)/AgentImplementation
TASK_DIR = $(SOURCES_DIR)/TaskImplementation
BENCH_DIR = $(SOURCES_DIR)/Benchmarks
OUTPUT_DIR = target

Orbit.exe: Common.dll AgentImplementation.dll TaskImplementation.dll \
 $(SOURCES_DIR)/OrbitLogic.fs $(BENCH_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs
	$(FSC) $(FSC_FLAGS) $(REFS) $(STANDALONE) -I:$(LIB) \
	-r $(OUTPUT_DIR)/Common.dll -r $(OUTPUT_DIR)/AgentImplementation.dll -r $(OUTPUT_DIR)/TaskImplementation.dll \
	-o:$(OUTPUT_DIR)/$@ $(SOURCES_DIR)/OrbitLogic.fs $(BENCH_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs

OrbitBigInt.exe: Common.dll AgentImplementation.dll TaskImplementation.dll \
 $(SOURCES_DIR)/OrbitLogic.fs $(BENCH_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs
	$(FSC) $(FSC_FLAGS) $(REFS) $(STANDALONE) -I:$(LIB) --define:BigInt \
	-r $(OUTPUT_DIR)/Common.dll -r $(OUTPUT_DIR)/AgentImplementation.dll -r $(OUTPUT_DIR)/TaskImplementation.dll \
	-o:$(OUTPUT_DIR)/$@ $(SOURCES_DIR)/OrbitLogic.fs $(BENCH_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs
	
AgentImplementation.dll: Common.dll $(AGENT_DIR)/Mapper.fs $(AGENT_DIR)/Aggregator.fs 
	$(FSC) $(FSC_FLAGS) $(REFS) -a -r $(OUTPUT_DIR)/Common.dll -I:$(LIB) \
	-o:$(OUTPUT_DIR)/$@ $(AGENT_DIR)/Mapper.fs $(AGENT_DIR)/Aggregator.fs 
	
TaskImplementation.dll: Common.dll $(TASK_DIR)/Mapper.fs $(TASK_DIR)/Aggregator.fs
	$(FSC) $(FSC_FLAGS) $(REFS) -a -r $(OUTPUT_DIR)/Common.dll -I:$(LIB) \
	-o:$(OUTPUT_DIR)/$@ $(TASK_DIR)/Mapper.fs $(TASK_DIR)/Aggregator.fs
	
Common.dll: $(COMMON_DIR)/Helpers.fs $(COMMON_DIR)/OrbitTypes.fs 
	$(FSC) $(FSC_FLAGS) $(REFS) -a -I:$(LIB) -o $(OUTPUT_DIR)/$@ $^
	
clean:
	$(RM) $(OUTPUT_DIR)/*.exe
