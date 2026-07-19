import json
import os
import re

def to_ts_type(csharp_type, ref=None):
    if ref:
        return ref.split('/')[-1]
    mapping = {
        'integer': 'number',
        'number': 'number',
        'string': 'string',
        'boolean': 'boolean',
        'array': 'any[]'
    }
    return mapping.get(csharp_type, 'any')

def generate_mock_value(prop_type, prop_name=None):
    if prop_type == 'string':
        if prop_name and 'email' in prop_name.lower():
            return "user@example.com"
        if prop_name and 'date' in prop_name.lower():
            return "2026-10-15T08:00:00Z"
        return "string"
    elif prop_type == 'integer' or prop_type == 'number':
        return 1
    elif prop_type == 'boolean':
        return True
    elif prop_type == 'array':
        return []
    return {}

def resolve_schema(schema, all_schemas):
    if '$ref' in schema:
        ref_name = schema['$ref'].split('/')[-1]
        return all_schemas.get(ref_name, {})
    return schema

def build_mock_json(schema, all_schemas):
    schema = resolve_schema(schema, all_schemas)
    if 'properties' not in schema:
        return {}
    
    mock = {}
    for prop_name, prop_details in schema['properties'].items():
        prop_camel = prop_name[:1].lower() + prop_name[1:]
        
        if '$ref' in prop_details:
            mock[prop_camel] = build_mock_json(prop_details, all_schemas)
        elif prop_details.get('type') == 'array':
            items = prop_details.get('items', {})
            if '$ref' in items:
                mock[prop_camel] = [build_mock_json(items, all_schemas)]
            else:
                mock[prop_camel] = [generate_mock_value(items.get('type'))]
        else:
            mock[prop_camel] = generate_mock_value(prop_details.get('type'), prop_name)
    return mock

def generate():
    with open('swagger.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    os.makedirs('Frontend_Assets', exist_ok=True)
    all_schemas = data.get('components', {}).get('schemas', {})
    
    http_lines = [
        "@baseUrl = http://localhost:5129",
        "@token = YOUR_TOKEN_HERE",
        "",
        "### Login to get token",
        "POST {{baseUrl}}/api/Auth/login",
        "Content-Type: application/json",
        "",
        '{"email": "admin@etr.com", "password": "123456"}',
        "",
        "### ----------------------------------------------------",
    ]

    paths = data.get('paths', {})
    
    grouped_paths = {}
    for path, methods in paths.items():
        for method, details in methods.items():
            tags = details.get('tags', ['Other'])
            module = tags[0]
            if module not in grouped_paths:
                grouped_paths[module] = []
            grouped_paths[module].append((method, path, details))

    for module, endpoints in grouped_paths.items():
        http_lines.append(f"### ================= MODULE: {module} =================")
        for method, path, details in endpoints:
            summary = details.get('summary', '').replace('\\n', ' ')
            http_lines.append(f"### {summary}")
            
            sample_path = path
            sample_path = re.sub(r'\{.*?\}', '1', sample_path)
            
            http_lines.append(f"{method.upper()} {{{{baseUrl}}}}{sample_path}")
            http_lines.append("Authorization: Bearer {{token}}")
            
            if method.lower() in ['post', 'put']:
                http_lines.append("Content-Type: application/json")
                http_lines.append("")
                
                # Extract request body schema
                req_body = details.get('requestBody', {}).get('content', {}).get('application/json', {}).get('schema', {})
                if req_body:
                    mock_json = build_mock_json(req_body, all_schemas)
                    http_lines.append(json.dumps(mock_json, indent=2))
                else:
                    http_lines.append("{}")
            http_lines.append("")

    with open('ETR_Endpoints.http', 'w', encoding='utf-8') as f:
        f.write('\n'.join(http_lines))

    # Generate api-collection.ts
    ts_lines = [
        "/**",
        " * ALL-IN-ONE API DEFINITIONS",
        " * Automatically generated from Swagger/OpenAPI spec.",
        " */",
        ""
    ]

    for schema_name, schema_details in all_schemas.items():
        if schema_name == 'ProblemDetails': continue 
        
        ts_lines.append(f"export interface {schema_name} {{")
        props = schema_details.get('properties', {})
        for prop_name, prop_details in props.items():
            prop_camel = prop_name[:1].lower() + prop_name[1:]
            
            prop_type = 'any'
            if '$ref' in prop_details:
                prop_type = to_ts_type(None, prop_details['$ref'])
            elif prop_details.get('type') == 'array':
                if '$ref' in prop_details.get('items', {}):
                    prop_type = to_ts_type(None, prop_details['items']['$ref']) + '[]'
                else:
                    prop_type = to_ts_type(prop_details['items'].get('type')) + '[]'
            else:
                prop_type = to_ts_type(prop_details.get('type'))
                
            nullable = "?" if prop_details.get('nullable') else ""
            ts_lines.append(f"  {prop_camel}{nullable}: {prop_type};")
        ts_lines.append("}")
        ts_lines.append("")

    ts_lines.append("export const API_ENDPOINTS = {")
    for module, endpoints in grouped_paths.items():
        for method, path, details in endpoints:
            parts = [p for p in path.split('/') if p and p != 'api']
            name_parts = []
            for p in parts:
                if p.startswith('{'):
                    name_parts.append('BY_' + p[1:-1].upper())
                else:
                    name_parts.append(p.upper())
            
            const_name = f"{method.upper()}_{'_'.join(name_parts)}"
            const_name = const_name.replace('-', '_') # sanitize
            
            path_params = re.findall(r'\{(.*?)\}', path)
            if path_params:
                args = ', '.join([f"{p}: number | string" for p in path_params])
                replaced_path = path
                for p in path_params:
                    replaced_path = replaced_path.replace(f"{{{p}}}", f"${{{p}}}")
                ts_lines.append(f"  {const_name}: ({args}) => `{replaced_path}`,")
            else:
                ts_lines.append(f"  {const_name}: '{path}',")
                
    ts_lines.append("};")
    ts_lines.append("")
    
    fetch_wrapper = """
export const apiClient = {
  async get<T>(url: string, token?: string): Promise<T> {
    return request<T>(url, { method: 'GET' }, token);
  },
  async post<T>(url: string, body: any, token?: string): Promise<T> {
    return request<T>(url, { method: 'POST', body: JSON.stringify(body) }, token);
  },
  async put<T>(url: string, body: any, token?: string): Promise<T> {
    return request<T>(url, { method: 'PUT', body: JSON.stringify(body) }, token);
  },
  async delete<T>(url: string, token?: string): Promise<T> {
    return request<T>(url, { method: 'DELETE' }, token);
  }
};

async function request<T>(url: string, config: RequestInit, token?: string): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const response = await fetch(url, { ...config, headers });
  if (!response.ok) {
    throw new Error(`API Error: ${response.statusText}`);
  }
  
  if (response.status === 204) return {} as T;
  
  return await response.json();
}
"""
    ts_lines.append(fetch_wrapper)

    with open('Frontend_Assets/api-collection.ts', 'w', encoding='utf-8') as f:
        f.write('\n'.join(ts_lines))

if __name__ == '__main__':
    generate()
