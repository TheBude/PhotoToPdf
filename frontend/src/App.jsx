import { useMemo, useState } from 'react';
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom';
import axios from 'axios';

const API_URL = 'http://localhost:5152';

function Navbar() {
  return (
    <header className="border-b border-slate-800 bg-slate-950 text-white">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
        <div>
          <p className="text-2xl font-bold tracking-tight">PhotoToPdf</p>
          <p className="text-xs text-slate-400">Personalized PDF workspace</p>
        </div>
        <nav className="flex gap-6 text-sm text-slate-200">
          <Link to="/">Home</Link>
          <Link to="/photo-to-pdf">Photo → PDF</Link>
          <Link to="/pdf-to-photo">PDF → Photo</Link>
          <Link to="/login">Login</Link>
        </nav>
      </div>
    </header>
  );
}

function HomePage() {
  return (
    <main className="mx-auto max-w-7xl px-6 py-16">
      <section className="grid gap-10 rounded-3xl border border-slate-800 bg-slate-900/80 p-10 shadow-2xl shadow-cyan-950/30 lg:grid-cols-2">
        <div>
          <span className="inline-flex rounded-full border border-cyan-500/40 bg-cyan-500/10 px-3 py-1 text-xs font-medium uppercase tracking-[0.2em] text-cyan-300">
            Smart converter
          </span>
          <h1 className="mt-6 text-5xl font-black tracking-tight text-white">Turn images into PDFs, and PDFs into your photo archive.</h1>
          <p className="mt-5 max-w-xl text-lg text-slate-300">
            Personalized user storage, secure account handling, and fast conversion tools for every customer.
          </p>
          <div className="mt-8 flex gap-4">
            <Link to="/photo-to-pdf" className="rounded-xl bg-cyan-500 px-6 py-3 font-semibold text-slate-950 transition hover:bg-cyan-400">Start converting</Link>
            <Link to="/login" className="rounded-xl border border-slate-700 px-6 py-3 font-semibold text-white transition hover:border-slate-500">Create account</Link>
          </div>
        </div>
        <div className="rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800 via-slate-900 to-cyan-950 p-6">
          <div className="grid gap-4">
            <div className="rounded-2xl bg-slate-700/50 p-4 text-white">
              <p className="text-xs uppercase tracking-[0.2em] text-slate-400">User profile</p>
              <p className="mt-3 text-2xl font-bold">Amina Aliyeva</p>
              <p className="text-sm text-slate-300">Personal space • Secure conversion history</p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="rounded-2xl bg-slate-800 p-4">
                <p className="text-slate-400">Files converted</p>
                <p className="mt-2 text-3xl font-bold text-cyan-300">284</p>
              </div>
              <div className="rounded-2xl bg-slate-800 p-4">
                <p className="text-slate-400">Storage</p>
                <p className="mt-2 text-3xl font-bold text-emerald-300">1.8 GB</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}

function AuthPage() {
  const [mode, setMode] = useState('login');
  const [form, setForm] = useState({ fullName: '', email: '', password: '' });
  const [message, setMessage] = useState('');

  const submit = async (event) => {
    event.preventDefault();
    const endpoint = mode === 'login' ? '/api/auth/login' : '/api/auth/register';
    const payload = mode === 'login'
      ? { email: form.email, password: form.password }
      : { fullName: form.fullName, email: form.email, password: form.password };

    try {
      const response = await axios.post(`${API_URL}${endpoint}`, payload);
      localStorage.setItem('token', response.data.token);
      setMessage(mode === 'login' ? 'Login successful!' : 'Registration successful!');
      window.location.href = '/';
    } catch (error) {
      setMessage(error.response?.data?.message || 'Something went wrong.');
    }
  };

  return (
    <main className="mx-auto max-w-md px-6 py-16">
      <div className="rounded-3xl border border-slate-800 bg-slate-900 p-8 shadow-xl shadow-slate-950/40">
        <div className="mb-6 flex gap-2 rounded-full border border-slate-700 bg-slate-800 p-1">
          <button type="button" onClick={() => setMode('login')} className={`flex-1 rounded-full px-4 py-2 font-medium ${mode === 'login' ? 'bg-cyan-500 text-slate-950' : 'text-slate-300'}`}>
            Login
          </button>
          <button type="button" onClick={() => setMode('register')} className={`flex-1 rounded-full px-4 py-2 font-medium ${mode === 'register' ? 'bg-cyan-500 text-slate-950' : 'text-slate-300'}`}>
            Register
          </button>
        </div>

        <form onSubmit={submit} className="space-y-4">
          {mode === 'register' && (
            <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} className="w-full rounded-xl border border-slate-700 bg-slate-800 px-4 py-3 text-white" placeholder="Full name" />
          )}
          <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className="w-full rounded-xl border border-slate-700 bg-slate-800 px-4 py-3 text-white" type="email" placeholder="Email" />
          <input value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} className="w-full rounded-xl border border-slate-700 bg-slate-800 px-4 py-3 text-white" type="password" placeholder="Password" />
          <button type="submit" className="w-full rounded-xl bg-cyan-500 px-4 py-3 font-semibold text-slate-950 hover:bg-cyan-400">
            {mode === 'login' ? 'Sign in' : 'Create account'}
          </button>
        </form>

        {message && <p className="mt-4 text-sm text-cyan-300">{message}</p>}
      </div>
    </main>
  );
}

function ConverterPage({ type }) {
  const [files, setFiles] = useState([]);
  const [status, setStatus] = useState('');

  const endpoint = useMemo(() => {
    return type === 'photo-to-pdf' ? '/api/convert/photo-to-pdf' : '/api/convert/pdf-to-images';
  }, [type]);

  const onSubmit = async (event) => {
    event.preventDefault();
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    try {
      const token = localStorage.getItem('token');
      const response = await axios.post(`${API_URL}${endpoint}`, formData, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'multipart/form-data'
        },
        responseType: 'blob'
      });

      const downloadUrl = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = type === 'photo-to-pdf' ? 'converted.pdf' : 'converted.zip';
      link.click();
      setStatus('File downloaded successfully.');
    } catch (error) {
      setStatus(error.response?.data?.message || 'Conversion failed.');
    }
  };

  return (
    <main className="mx-auto max-w-4xl px-6 py-16">
      <div className="rounded-3xl border border-slate-800 bg-slate-900 p-8">
        <h2 className="text-3xl font-bold text-white">{type === 'photo-to-pdf' ? 'Photo to PDF' : 'PDF to Photo export'}</h2>
        <p className="mt-3 text-slate-300">Upload your files and start converting with your personal user profile.</p>

        <form onSubmit={onSubmit} className="mt-8 space-y-6">
          <input type="file" multiple={type === 'photo-to-pdf'} onChange={(e) => setFiles(Array.from(e.target.files || []))} className="block w-full cursor-pointer rounded-xl border border-dashed border-slate-700 bg-slate-800 p-4 text-slate-200" />
          <button type="submit" className="rounded-xl bg-cyan-500 px-6 py-3 font-semibold text-slate-950 hover:bg-cyan-400">Convert</button>
        </form>

        {status && <p className="mt-6 text-cyan-300">{status}</p>}
      </div>
    </main>
  );
}

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-slate-950 text-white">
        <Navbar />
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<AuthPage />} />
          <Route path="/photo-to-pdf" element={<ConverterPage type="photo-to-pdf" />} />
          <Route path="/pdf-to-photo" element={<ConverterPage type="pdf-to-images" />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
