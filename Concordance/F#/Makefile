FSC=fsharpc
FSC_FLAGS = --optimize
SOURCES_DIR = src
OUTPUT_DIR = target

Concordance.exe: $(SOURCES_DIR)/Helpers.fs $(SOURCES_DIR)/Transformation.fs $(SOURCES_DIR)/Concordance.fs $(SOURCES_DIR)/Program.fs
	$(FSC) $(FSC_FLAGS) -o $(OUTPUT_DIR)/$@ $^
	
clean:
	$(RM) $(OUTPUT_DIR)/*.exe
