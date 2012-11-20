FSC=fsharpc
FSC_FLAGS = --optimize
SOURCES_DIR = src
OUTPUT_DIR = target

Orbit.exe: $(SOURCES_DIR)/Helpers.fs $(SOURCES_DIR)/OrbitTypes.fs $(SOURCES_DIR)/Mapper.fs $(SOURCES_DIR)/Aggregator.fs $(SOURCES_DIR)/OrbitLogic.fs $(SOURCES_DIR)/OrbitBench.fs $(SOURCES_DIR)/Orbit.fs
	$(FSC) $(FSC_FLAGS) -o $(OUTPUT_DIR)/$@ $^
	
clean:
	$(RM) $(OUTPUT_DIR)/*.exe
