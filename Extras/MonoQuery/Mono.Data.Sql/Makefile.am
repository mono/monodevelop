
FILES =  \
	AssemblyInfo.cs \
	Providers/DbProviderBase.cs \
	Providers/FirebirdDbProvider.cs \
	Providers/MySqlDbProvider.cs \
	Providers/NpgsqlDbProvider.cs \
	Providers/OdbcDbProvider.cs \
	Providers/OracleDbProvider.cs \
	Providers/SqlDbProvider.cs \
	Providers/SqliteDbProvider.cs \
	Providers/SybaseDbProvider.cs \
	Schema/AbstractSchema.cs \
	Schema/AggregateSchema.cs \
	Schema/CheckConstraintSchema.cs \
	Schema/ColumnSchema.cs \
	Schema/ConstraintSchema.cs \
	Schema/DataTypeSchema.cs \
	Schema/ForeignKeyConstraintSchema.cs \
	Schema/GroupSchema.cs \
	Schema/IndexConstraintSchema.cs \
	Schema/ISchema.cs \
	Schema/LanguageSchema.cs \
	Schema/OperatorSchema.cs \
	Schema/ParameterSchema.cs \
	Schema/PrimaryKeyConstraintSchema.cs \
	Schema/PrivilegeSchema.cs \
	Schema/ProcedureSchema.cs \
	Schema/RoleSchema.cs \
	Schema/RuleSchema.cs \
	Schema/SchemaSchema.cs \
	Schema/SequenceSchema.cs \
	Schema/TableSchema.cs \
	Schema/TriggerSchema.cs \
	Schema/UniqueConstraintSchema.cs \
	Schema/UserSchema.cs \
	Schema/ViewSchema.cs

REFS =  \
	-r:$(top_srcdir)/contrib/FirebirdSql.Data.Firebird.dll \
	-r:ByteFX.Data \
	-r:Mono.Data.SqliteClient \
	-r:Mono.Data.SybaseClient \
	-r:Npgsql \
	-r:System \
	-r:System.Data \
	-r:System.Data.OracleClient


ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoQuery

ASSEMBLY = $(ADDIN_BUILD)/Mono.Data.Sql.dll

if ENABLE_MONOQUERY
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(SCS_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

if ENABLE_MONOQUERY
monoquerydir = $(MD_ADDIN_DIR)/MonoQuery
monoquery_DATA = $(ASSEMBLY)
endif

include $(top_srcdir)/Makefile.include
