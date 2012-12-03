FSC=fsharpc
FSC_FLAGS = --optimize
REFS = -r System.dll -r System.Core.dll -r System.Numerics.dll
LIB = $(HOME)/usr/lib/mono/4.5/
STANDALONE = --standalone
SOURCES_DIR = src
COMMON_DIR = $(SOURCES_DIR)/Common
AGENT_DIR = $(SOURCES_DIR)/AgentImplementation
BENCH_DIR = $(SOURCES_DIR)/Benchmarks
OUTPUT_DIR = target

Orbit.exe: $(COMMON_DIR)/Helpers.fs $(COMMON_DIR)/OrbitTypes.fs $(AGENT_DIR)/Mapper.fs $(AGENT_DIR)/Aggregator.fs $(SOURCES_DIR)/OrbitLogic.fs $(BENCH_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs
	$(FSC) $(FSC_FLAGS) $(REFS) -I:$(LIB) $(STANDALONE) -o $(OUTPUT_DIR)/$@ $^
	
clean:
	$(RM) $(OUTPUT_DIR)/*.exe
