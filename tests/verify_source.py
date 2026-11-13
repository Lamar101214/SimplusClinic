"""Structural checks only. This does NOT compile C# or execute SQL."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
checks=0
def check(condition,label):
 global checks
 assert condition,label
 checks+=1
for path in list(root.rglob('*.csproj'))+list(root.rglob('App.config'))+[root/'SimplusClinic.slnx']:
 tree=ET.parse(path)
 for node in tree.findall('.//ProjectReference'):
  check((path.parent/node.attrib['Include']).is_file(),f'Missing reference {path}')
 for node in tree.findall('.//Project'):
  if 'Path' in node.attrib:check((root/node.attrib['Path']).is_file(),'Missing solution project')
 checks+=1
schema=(root/'Database/01_CreateDatabase.sql').read_text()
procs=(root/'Database/02_ProceduresAndPermissions.sql').read_text()
editors=(root/'src/SimplusClinic.WinForms/Forms/EditorFields.cs').read_text()
for name in ['Patient','Doctor','MedicalRecord','Appointment']:
 model=(root/f'src/SimplusClinic.Domain/{name}.cs').read_text()
 props=set(re.findall(r'public (?:string|int|bool|DateTime) (\w+) \{ get; set; \}',model))
 fields=props-{'PatientName','DoctorName'}
 repo=(root/f'src/SimplusClinic.Infrastructure/{name}Repository.cs').read_text()
 proc=procs.split(f'CREATE OR ALTER PROCEDURE dbo.Save{name}\n')[1].split('\nGO')[0]
 for f in fields:
  check(re.search(r'\b'+f+r'\s+(?:nvarchar|int|bit|date|datetime2)',schema),f'Missing SQL column {name}.{f}')
  check(f'@{f} ' in proc,f'Missing procedure parameter {name}.{f}')
  check(f'"@{f}"' in repo,f'Missing repository binding {name}.{f}')
  check(f'{f}=' in repo,f'Missing reader mapping {name}.{f}')
  if f!='IsArchived':check(f'new("{f}"' in editors,f'Missing editor {name}.{f}')
 check('sp_getapplock' in proc,f'Missing serialization {name}')
 check('Version=@Version' in proc,f'Missing optimistic concurrency {name}')
 check('ROLLBACK' in proc and 'COMMIT' in proc,f'Missing transaction handling {name}')
 check(f'DENY INSERT, UPDATE, DELETE ON dbo.{name}s' in procs,f'Missing write restriction {name}')
for pair in [('MedicalRecords','Patients'),('MedicalRecords','Doctors'),('Appointments','Patients'),('Appointments','Doctors')]:
 check('FK_'+'_'.join(pair) in schema,'Missing FK '+str(pair))
check('SYSTEM_VERSIONING = ON' in schema,'Missing temporal history')
check('StartAt < @EndAt AND EndAt > @StartAt' in procs,'Incorrect interval predicate')
check('DoctorId=@DoctorId OR PatientId=@PatientId' in procs,'Missing resource check')
check('Status<>N\'Cancelled\'' in procs,'Missing cancelled exclusion')
check('Id<>@Id' in procs,'Missing update self-exclusion')
check(schema.startswith(':ON ERROR EXIT'),'Schema must stop on existing DB')
# Optional tree-sitter syntax scan; not a substitute for compilation/type checking.
try:
 import tree_sitter,tree_sitter_c_sharp
 parser=tree_sitter.Parser(tree_sitter.Language(tree_sitter_c_sharp.language()))
 for path in root.rglob('*.cs'):
  tree=parser.parse(path.read_bytes())
  check(not tree.root_node.has_error,f'C# syntax parse error: {path}')
 print('C# grammar scan: passed')
except ImportError:
 print('C# grammar scan: skipped (optional tree-sitter dependencies absent)')
print(f'{checks} structural checks passed. Compilation and runtime tests remain required.')
