FILES = \
Schema/IndexConstraintSchema.cs \
Schema/OperatorSchema.cs \
Schema/GroupSchema.cs \
Schema/ColumnSchema.cs \
Schema/SchemaSchema.cs \
Schema/RoleSchema.cs \
Schema/ISchema.cs \
Schema/PrimaryKeyConstraintSchema.cs \
Schema/AbstractSchema.cs \
Schema/TriggerSchema.cs \
Schema/RuleSchema.cs \
Schema/ForeignKeyConstraintSchema.cs \
Schema/SequenceSchema.cs \
Schema/ViewSchema.cs \
Schema/DataTypeSchema.cs \
Schema/UniqueConstraintSchema.cs \
Schema/UserSchema.cs \
Schema/ParameterSchema.cs \
Schema/CheckConstraintSchema.cs \
Schema/LanguageSchema.cs \
Schema/ConstraintSchema.cs \
Schema/PrivilegeSchema.cs \
Schema/AggregateSchema.cs \
Schema/TableSchema.cs \
Schema/ProcedureSchema.cs \
Providers/MySqlDbProvider.cs \
Providers/DbProviderBase.cs \
Providers/NpgsqlDbProvider.cs \
Providers/SqliteDbProvider.cs \
Providers/OracleDbProvider.cs \
Providers/FirebirdDbProvider.cs \
Providers/OdbcDbProvider.cs \
Providers/SybaseDbProvider.cs \
Providers/SqlDbProvider.cs \
AssemblyInfo.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

REFS = \
-r:System.Data \
-r:Npgsql \
-r:ByteFX.Data \
-r:Mono.Data.SqliteClient \
-r:System.Data.OracleClient \
-r:Mono.Data.SybaseClient \
-r:$(top_srcdir)/contrib/FirebirdSql.Data.Firebird.dll

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoQuery

DLL = $(ADDIN_BUILD)/Mono.Data.Sql.dll

all: $(DLL)

$(DLL): $(FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(SCS_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES)

monoquerydir = $(MD_ADDIN_DIR)/MonoQuery
monoquery_DATA = $(DLL)
