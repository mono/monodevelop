COND_FILES=
COND_REFS=

if ENABLE_POSTGRES
COND_FILES += $(srcdir)/Providers/NpgsqlDbProvider.cs
COND_REFS += -r:Npgsql
endif

if ENABLE_MYSQL
COND_FILES += $(srcdir)/Providers/MySqlDbProvider.cs
COND_REFS += -r:ByteFX.Data
endif

if ENABLE_SQLITE
COND_FILES += $(srcdir)/Providers/SqliteDbProvider.cs
COND_REFS += -r:Mono.Data.SqliteClient
endif

if ENABLE_ORACLE
COND_FILES += $(srcdir)/Providers/OracleDbProvider.cs
COND_REFS += -r:System.Data.OracleClient
endif

if ENABLE_SQLSERV
COND_FILES += $(srcdir)/Providers/SqlDbProvider.cs
endif

if ENABLE_SYBASE
COND_FILES += $(srcdir)/Providers/SybaseDbProvider.cs
COND_REFS += -r:Mono.Data.SybaseClient
endif

FILES =  \
	$(COND_FILES) \
	AssemblyInfo.cs \
	Providers/DbProviderBase.cs \
	Providers/FirebirdDbProvider.cs \
	Providers/OdbcDbProvider.cs \
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
	$(COND_REFS) \
	-r:$(top_srcdir)/contrib/Firebird/FirebirdSql.Data.Firebird.dll \
	-r:System \
	-r:System.Data

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoQuery

ASSEMBLY = $(ADDIN_BUILD)/Mono.Data.Sql.dll

if ENABLE_MONOQUERY
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(COND_FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(SCS_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

if ENABLE_MONOQUERY
monoquerydir = $(MD_ADDIN_DIR)/MonoQuery
monoquery_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

include $(top_srcdir)/Makefile.include
