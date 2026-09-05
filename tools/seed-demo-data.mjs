/**
 * Fills an empty Gym Manager database with demo data: membership plans, trainers, shop products,
 * members, memberships and payments. Enough that the dashboard, reports and stock alerts have
 * something real to show instead of rendering zeros.
 *
 *   node tools/seed-demo-data.mjs
 *
 * Override with environment variables if the API is not on the default port or the admin
 * credentials differ:
 *
 *   GYM_API_URL   default http://localhost:8080/api/v1
 *   GYM_EMAIL     default admin@gymmanager.local
 *   GYM_PASSWORD  default Admin@12345
 *
 * Two things worth knowing before editing it.
 *
 * It goes through the HTTP API rather than SQL. That is slower, and it is the point: validation,
 * permissions and domain rules all run, so this cannot create a row the application itself would
 * reject. Seeding straight into tables produces data that looks fine in the database and renders
 * as broken screens.
 *
 * Enums are sent as numbers. The API has no JsonStringEnumConverter, so "Male" is a 400 while 1
 * is accepted. The numeric values are noted at each use.
 *
 * Re-running is safe. Anything already present comes back as a duplicate and is counted, not
 * treated as a failure.
 */

const API = process.env.GYM_API_URL ?? 'http://localhost:8080/api/v1';
const EMAIL = process.env.GYM_EMAIL ?? 'admin@gymmanager.local';
const PASSWORD = process.env.GYM_PASSWORD ?? 'Admin@12345';

let token = '';
const stats = { created: 0, existed: 0, failed: [] };

async function call(method, path, body) {
  let res;
  try {
    res = await fetch(API + path, {
      method,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (cause) {
    throw new Error(`Could not reach the API at ${API}. Is it running?`, { cause });
  }
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { /* problem+json or empty body */ }
  return { ok: res.ok, status: res.status, json, text };
}

async function create(label, path, body) {
  const r = await call('POST', path, body);
  if (r.ok) { stats.created++; return r.json; }
  if (r.status === 409 || /already|duplicate|exists/i.test(r.text)) { stats.existed++; return null; }
  stats.failed.push(`${label}: ${r.status} ${r.text.slice(0, 160)}`);
  return null;
}

/** Both shapes appear across these endpoints: a bare array, or { items: [...] }. */
const listOf = res => (Array.isArray(res.json) ? res.json : res.json?.items ?? []);

const FIRST = ['Omar', 'Sara', 'Youssef', 'Nour', 'Karim', 'Hana', 'Tarek', 'Mona', 'Ziad', 'Layla',
               'Ahmed', 'Farah', 'Hassan', 'Dina', 'Amir', 'Rania', 'Sherif', 'Yasmin'];
const LAST = ['Hassan', 'Ibrahim', 'Mahmoud', 'Fouad', 'Salah', 'Nabil', 'Adel', 'Rashad', 'Zaki', 'Mansour'];
const CITIES = ['Cairo', 'Giza', 'Alexandria'];

async function main() {
  const login = await call('POST', '/auth/login', { email: EMAIL, password: PASSWORD });
  if (!login.ok) {
    throw new Error(`Login failed for ${EMAIL} (${login.status}). ${login.text.slice(0, 200)}`);
  }
  token = login.json.authentication.accessToken;

  // Discovered, never hard-coded: branch ids differ per database, and a pinned GUID makes this
  // script work on exactly one machine.
  const branches = listOf(await call('GET', '/branches'));
  if (!branches.length) throw new Error('No branches exist. Run the app once so the default branch is seeded.');
  const branchId = branches[0].id;

  // --- Membership plans ----------------------------------------------------
  for (const p of [
    { name: 'Monthly',   description: 'Full gym access, one month.',            price: 450,  durationInDays: 30,  maxFreezeDays: 7 },
    { name: 'Quarterly', description: 'Three months, includes one guest pass.', price: 1200, durationInDays: 90,  maxFreezeDays: 14 },
    { name: 'Annual',    description: 'Twelve months, all classes included.',   price: 4000, durationInDays: 365, maxFreezeDays: 30 },
    { name: 'Off-Peak',  description: 'Weekdays before 4pm.',                   price: 300,  durationInDays: 30,  maxFreezeDays: 5 },
  ]) {
    await create(`plan ${p.name}`, '/membership-plans', { ...p, currency: 'EGP', branchId });
  }
  // Read back rather than collecting ids from the POSTs, so a re-run still finds the plans it
  // did not create this time.
  const planIds = listOf(await call('GET', '/membership-plans')).map(p => p.id);

  // --- Trainers ------------------------------------------------------------
  const trainers = [
    { firstName: 'Khaled', lastName: 'Sabry',  specialization: 'Strength & Conditioning', bio: 'Ten years coaching powerlifting and Olympic lifts.' },
    { firstName: 'Mariam', lastName: 'Adel',   specialization: 'Yoga & Mobility',         bio: 'Vinyasa and restorative yoga; rehabilitation background.' },
    { firstName: 'Tamer',  lastName: 'Gaber',  specialization: 'HIIT & Cardio',           bio: 'Former national middle-distance runner.' },
    { firstName: 'Salma',  lastName: 'Naguib', specialization: 'Nutrition & Wellness',    bio: 'Registered dietitian working with athletes.' },
  ];
  for (const [i, t] of trainers.entries()) {
    await create(`trainer ${t.lastName}`, '/trainers', {
      ...t,
      branchId,
      phoneNumber: `0100000${String(10 + i).padStart(4, '0')}`,
      email: `${t.firstName.toLowerCase()}.${t.lastName.toLowerCase()}@gymmanager.local`,
      userId: null,
    });
  }

  // --- Shop products -------------------------------------------------------
  // ProductCategory: Supplement=0, Apparel=1, Accessory=2, Beverage=3, Other=4.
  // Two items are deliberately below their reorder threshold so the low-stock panel is populated.
  for (const p of [
    { name: 'Whey Protein 1kg',    sku: 'SUP-WHEY-1K',  category: 0, price: 850, costPrice: 620, initialStock: 24, reorderThreshold: 6 },
    { name: 'Creatine 300g',       sku: 'SUP-CREA-300', category: 0, price: 420, costPrice: 300, initialStock: 18, reorderThreshold: 5 },
    { name: 'Shaker Bottle',       sku: 'ACC-SHAKER',   category: 2, price: 120, costPrice: 60,  initialStock: 40, reorderThreshold: 10 },
    { name: 'Lifting Straps',      sku: 'ACC-STRAPS',   category: 2, price: 250, costPrice: 140, initialStock: 15, reorderThreshold: 5 },
    { name: 'Resistance Band Set', sku: 'ACC-BANDS',    category: 2, price: 380, costPrice: 210, initialStock: 4,  reorderThreshold: 8 },
    { name: 'Electrolyte Drink',   sku: 'BEV-ELEC',     category: 3, price: 45,  costPrice: 22,  initialStock: 60, reorderThreshold: 20 },
    { name: 'Protein Bar',         sku: 'BEV-BAR',      category: 3, price: 65,  costPrice: 35,  initialStock: 3,  reorderThreshold: 12 },
  ]) {
    await create(`product ${p.sku}`, '/products', { ...p, description: p.name, currency: 'EGP', branchId });
  }

  // --- Members -------------------------------------------------------------
  // Gender: Unspecified=0, Male=1, Female=2.
  for (let i = 0; i < 24; i++) {
    const first = FIRST[i % FIRST.length];
    const last = LAST[(i * 3) % LAST.length];
    await create(`member ${i}`, '/members', {
      branchId,
      firstName: first,
      lastName: last,
      phoneNumber: `01${String(100000000 + i * 7919).slice(0, 9)}`,
      email: `${first.toLowerCase()}.${last.toLowerCase()}${i}@example.com`,
      dateOfBirth: `19${75 + (i % 25)}-0${1 + (i % 9)}-1${i % 9}`,
      gender: i % 2 === 0 ? 1 : 2,
      street: `${10 + i} Nile Street`,
      city: CITIES[i % 3],
      state: CITIES[i % 3],
      postalCode: `1${String(1000 + i).slice(0, 4)}`,
      country: 'Egypt',
      emergencyContactName: `${LAST[(i + 4) % LAST.length]} family`,
      emergencyContactPhone: `0111${String(1000000 + i * 137).slice(0, 7)}`,
    });
  }
  const members = listOf(await call('GET', '/members?pageSize=100'));

  // Memberships and payments carry no natural key, so unlike the entities above they cannot be
  // de-duplicated by the API -- a second run just adds another set and doubles the revenue on the
  // dashboard. Skip both if any payment already exists, which is the marker that this script has
  // run against this database before.
  const alreadySeeded = listOf(await call('GET', '/payments?pageSize=1')).length > 0;
  if (alreadySeeded) {
    console.log('Memberships and payments already present; leaving them alone.');
    report();
    return;
  }

  // --- Memberships ---------------------------------------------------------
  // Every fifth member is left without one. A gym where 100% of members are active is not a
  // screen worth designing reports against.
  if (planIds.length) {
    for (const [i, m] of members.entries()) {
      if (i % 5 === 4) continue;
      const start = new Date();
      start.setDate(start.getDate() - ((i * 5) % 120));
      await create(`membership ${i}`, '/memberships', {
        memberId: m.id,
        membershipPlanId: planIds[i % planIds.length],
        startDate: start.toISOString().slice(0, 10),
      });
    }
  }

  // --- Payments ------------------------------------------------------------
  // Without these the revenue cards read 0.00 with members on screen, which looks broken rather
  // than empty. PaymentMethod: Cash=0, Card=1, BankTransfer=2.
  // PaymentReferenceType: MembershipPurchase=0, ProductSale=3.
  const fees = [450, 1200, 4000, 300];
  for (const [i, m] of members.entries()) {
    if (i % 5 === 4) continue;
    await create(`membership fee ${i}`, '/payments', {
      memberId: m.id, branchId, amount: fees[i % fees.length], currency: 'EGP',
      method: i % 3, referenceType: 0, referenceId: null,
    });
  }
  for (const [i, m] of members.slice(0, 9).entries()) {
    await create(`product sale ${i}`, '/payments', {
      memberId: m.id, branchId, amount: [120, 850, 65, 420, 250][i % 5], currency: 'EGP',
      method: i % 2, referenceType: 3, referenceId: null,
    });
  }

  report();
}

function report() {
  console.log(`created ${stats.created}, already present ${stats.existed}, failed ${stats.failed.length}`);
  stats.failed.slice(0, 10).forEach(f => console.log('  ! ' + f));
  if (stats.failed.length) process.exitCode = 1;
}

main().catch(err => {
  console.error(err.message);
  if (err.cause) console.error('  cause:', err.cause.message);
  process.exit(1);
});
