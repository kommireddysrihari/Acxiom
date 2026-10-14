"""End-to-end acceptance smoke test. Uses only Python's standard library.
Run against a LOCAL throwaway database. Creates persistent test records.
ACXIOM_ADMIN_EMAIL and ACXIOM_ADMIN_PASSWORD must identify the bootstrap admin.
"""
import datetime as dt
import http.cookiejar
import json
import os
import secrets
import sys
import urllib.error
import urllib.parse
import urllib.request
from html.parser import HTMLParser

BASE = os.environ.get('ACXIOM_BASE_URL', 'http://localhost:5080').rstrip('/')
if urllib.parse.urlparse(BASE).hostname not in ('localhost', '127.0.0.1'):
    sys.exit('This test only supports localhost to avoid changing a deployed database.')

class TokenParser(HTMLParser):
    token = ''
    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == 'input' and a.get('name') == '__RequestVerificationToken':
            self.token = a.get('value', '')

class Client:
    def __init__(self):
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        self.csrf = ''
    def request(self, path, method='GET', payload=None, json_body=False, csrf=True):
        headers = {}
        body = None
        if payload is not None:
            body = (json.dumps(payload) if json_body else urllib.parse.urlencode(payload)).encode()
            headers['Content-Type'] = 'application/json' if json_body else 'application/x-www-form-urlencoded'
        if method not in ('GET', 'HEAD') and csrf and self.csrf:
            headers['X-CSRF-TOKEN'] = self.csrf
        try:
            r = self.opener.open(urllib.request.Request(BASE+path, data=body, headers=headers, method=method), timeout=30)
        except urllib.error.HTTPError as e:
            r = e
        text = r.read().decode('utf-8-sig')
        try: data = json.loads(text)
        except json.JSONDecodeError: data = text
        return r.status, data
    def form(self, path, values):
        code, page = self.request(path)
        assert code == 200, (path, code)
        parser = TokenParser(); parser.feed(page)
        assert parser.token, 'Missing form antiforgery token'
        return self.request(path, 'POST', {**values, '__RequestVerificationToken': parser.token})
    def token(self):
        code, data = self.request('/api/csrf'); assert code == 200, code
        self.csrf = data['requestToken']
    def api(self, kind, payload, method='POST', suffix=''):
        return self.request('/api/'+kind+suffix, method, payload, True)

def expect(code, expected, label):
    assert code == expected, f'{label}: expected {expected}, got {code}'
    print('PASS', label)

def owner_id(client):
    code, html = client.request('/crm/customers/new'); assert code == 200
    class SelectParser(HTMLParser):
        inside = False
        result = ''
        def handle_starttag(self, tag, attrs):
            a = dict(attrs)
            if tag == 'select': self.inside = a.get('name') == 'AssignedTo'
            if tag == 'option' and self.inside: self.result = a.get('value', '')
        def handle_endtag(self, tag):
            if tag == 'select': self.inside = False
    parser = SelectParser();parser.feed(html);assert parser.result
    return parser.result

admin_email = os.environ.get('ACXIOM_ADMIN_EMAIL')
admin_password = os.environ.get('ACXIOM_ADMIN_PASSWORD')
if not admin_email or not admin_password:
    sys.exit('Set ACXIOM_ADMIN_EMAIL and ACXIOM_ADMIN_PASSWORD in your terminal first.')
nonce = secrets.token_hex(5)
password = 'Aa9!'+secrets.token_urlsafe(18)
anon = Client()
expect(anon.request('/api/customers')[0], 401, 'anonymous API access blocked')
admin = Client(); admin.form('/Account/Login', {'Login': admin_email, 'Password': admin_password}); admin.token()
expect(admin.request('/Admin')[0], 200, 'admin user administration')
clients = []
for i in range(2):
    c = Client(); email = f'sales{i}-{nonce}@example.com'
    c.form('/Account/Register', {'Email': email, 'Password': password, 'ConfirmPassword': password})
    c.token(); clients.append((c, owner_id(c), email))
a, aid, aemail = clients[0]; b, bid, bemail = clients[1]
expect(a.request('/Admin')[0], 403, 'sales cannot access user administration')
expect(a.request('/Admin/Audit')[0], 403, 'sales cannot access audit administration')
phone = '9'+str(secrets.randbelow(10**9)).zfill(9)
def customer(owner, email, mobile):
    return {'kind':'customers','name':'Interview Demo '+nonce,'email':email,'phone':mobile,'assignedTo':owner,'status':'Active','companyName':'Demo Company'}
ca = customer(aid, aemail, phone)
code, row = a.api('customers', ca);expect(code, 201, 'customer create')
expect(a.api('customers', ca)[0], 409, 'duplicate customer rejected')
expect(a.api('customers', {**ca,'email':'not-email'})[0], 400, 'invalid email rejected server-side')
expect(a.api('customers', {**ca,'phone':'123'})[0], 400, 'invalid phone rejected server-side')
expect(a.api('customers', {**ca,'name':''})[0], 400, 'required name rejected')
expect(a.api('customers', {**ca,'assignedTo':bid})[0], 403, 'sales cannot assign another owner')
expect(b.request('/api/customers/'+row['id'])[0], 404, 'cross-owner read blocked')
expect(b.api('customers', {**ca,'assignedTo':bid,'version':row['version']}, 'PUT', '/'+row['id'])[0], 404, 'cross-owner update blocked')
expect(a.request('/api/customers','POST',ca,True,False)[0],400,'missing antiforgery rejected')
expect(b.request('/api/customers')[1]['total'],0,'list excludes other owner records')
tomorrow = (dt.date.today()+dt.timedelta(days=2)).isoformat()
yesterday = (dt.date.today()-dt.timedelta(days=2)).isoformat()
op = {'kind':'opportunities','name':'Pilot contract','status':'Open','assignedTo':aid,'customerId':row['id'],'amount':100000,'probability':50,'stage':'Proposal','expectedCloseDate':tomorrow}
for field, value in [('amount',0),('amount',-5),('probability',101),('expectedCloseDate',yesterday)]:
    expect(a.api('opportunities',{**op,field:value})[0],400,'opportunity rejects '+field+'='+str(value))
code, opportunity = a.api('opportunities',op);expect(code,201,'valid opportunity create')
follow={'kind':'followups','name':'Discovery call','status':'Planned','assignedTo':aid,'customerId':row['id'],'followUpDate':yesterday,'followUpType':'Call'}
expect(a.api('followups',follow)[0],400,'past follow-up rejected')
code, followrow=a.api('followups',{**follow,'followUpDate':tomorrow});expect(code,201,'follow-up create')
code, updated=a.api('followups',{**followrow,'status':'Completed'},'PUT','/'+followrow['id']);expect(code,200,'follow-up completion')
lead={'kind':'leads','name':'Conversion lead','email':'lead-'+nonce+'@example.com','phone':'8'+str(secrets.randbelow(10**9)).zfill(9),'assignedTo':aid,'status':'New','expectedValue':50000,'source':'Website'}
code, leadrow=a.api('leads',lead);expect(code,201,'lead create')
expect(a.api('leads',{**leadrow,'status':'Converted'},'PUT','/'+leadrow['id'])[0],400,'conversion cannot be bypassed')
for state in ('Contacted','Qualified'):
    code, leadrow=a.api('leads',{**leadrow,'status':state},'PUT','/'+leadrow['id']);expect(code,200,'lead transition '+state)
expect(a.request('/api/leads/'+leadrow['id']+'/convert?version='+leadrow['version'],'POST')[0],200,'transactional lead conversion')
expect(a.request('/api/leads/'+leadrow['id']+'/convert?version='+leadrow['version'],'POST')[0],409,'double conversion rejected')
expect(a.api('customers',{**row,'name':'Updated demo'},'PUT','/'+row['id'])[0],200,'customer edit')
expect(a.api('customers',{**row,'name':'Stale update'},'PUT','/'+row['id'])[0],409,'stale update rejected')
expect(a.request('/api/customers/'+row['id']+'?version='+row['version'],'DELETE')[0],409,'stale delete rejected')
for path in ('/','/crm/customers','/crm/leads','/crm/opportunities','/crm/followups','/crm/activities','/Reports','/Reports?kind=pipeline','/Reports?kind=conversion','/Reports?export=true','/api/reports/pipeline'):
    expect(a.request(path)[0],200,'page / report '+path)
# Create a manager through admin UI (role changes and user-create audit events).
manager_email='manager-'+nonce+'@example.com'
admin.form('/Admin/Edit',{'Email':manager_email,'Role':'Manager','IsActive':'true','Password':password})
manager=Client();manager.form('/Account/Login',{'Login':manager_email,'Password':password});manager.token()
expect(manager.request('/api/customers/'+row['id'])[0],200,'manager business scope')
expect(manager.request('/Admin')[0],403,'manager cannot administer security')
expect(manager.request('/Reports?kind=useractivity')[0],200,'manager activity report')

# Additional lifecycle checks: successful deletion, activities, and active-session revocation.
activity={'kind':'activities','name':'Meeting notes','status':'Planned','assignedTo':aid,'customerId':row['id'],'activityDate':tomorrow,'activityType':'Meeting'}
code, activityrow=a.api('activities',activity);expect(code,201,'activity create')
code, activityrow=a.api('activities',{**activityrow,'status':'Completed'},'PUT','/'+activityrow['id']);expect(code,200,'activity completion')
expect(a.request('/api/activities/'+activityrow['id']+'?version='+activityrow['version'],'DELETE')[0],204,'activity delete')
expect(a.request('/api/activities/'+activityrow['id'])[0],404,'deleted activity no longer readable')
code, won=a.api('opportunities',{**opportunity,'stage':'Won','status':'Won','probability':100},'PUT','/'+opportunity['id']);expect(code,200,'mark opportunity won')
assert won['closedDate'], 'Actual close timestamp missing'
print('PASS won opportunity has actual close timestamp')
code,temp=a.api('customers',customer(aid,'temp-'+nonce+'@example.com','7'+str(secrets.randbelow(10**9)).zfill(9)));expect(code,201,'temporary customer create')
expect(a.request('/api/customers/'+temp['id']+'?version='+temp['version'],'DELETE')[0],204,'customer delete')
admin.form('/Admin/Edit?id='+bid,{'Id':bid,'Email':bemail,'Role':'SalesExecutive','IsActive':'false'})
expect(b.request('/api/customers')[0],401,'deactivation revokes existing session')
admin.form('/Admin/Edit?id='+bid,{'Id':bid,'Email':bemail,'Role':'SalesExecutive','IsActive':'true'})

expect(admin.request('/Admin/Audit')[0],200,'audit page renders')
code,auditcsv=admin.request('/Reports?kind=audit&export=true');expect(code,200,'audit CSV export')
assert all(event in auditcsv for event in ('Convert','Create','Update','Delete'))
assert password not in auditcsv and 'PasswordHash' not in auditcsv
print('PASS audit contains business events and excludes test password')
# Lockout is last: all clients share an IP and the auth limiter allows 20 requests/minute.
# This test stays under 20 POSTs but GETs also count; wait is explicit here in the user-run script.
import time
print('Waiting 61 seconds for the auth rate-limit window before lockout checks…')
time.sleep(61)
locked=Client()
for _ in range(5): locked.form('/Account/Login',{'Login':bemail,'Password':'Incorrect!12345'})
locked.form('/Account/Login',{'Login':bemail,'Password':password})
expect(locked.request('/api/customers')[0],401,'five failed logins lock the account')
print('\nAll smoke checks passed. Demo records remain in the local database. Complete the manual browser checklist too.')
