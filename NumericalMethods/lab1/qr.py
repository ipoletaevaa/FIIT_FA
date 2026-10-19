import math


def copy_matrix(a):
    result = []
    for row in a:
        result.append(row[:])
    return result


def multiply(a, b):
    n = len(a)
    m = len(b[0])
    k = len(b)
    product = []
    for i in range(n):
        row = []
        for j in range(m):
            value = 0.0
            for t in range(k):
                value += a[i][t] * b[t][j]
            row.append(value)
        product.append(row)
    return product


def column(a, j):
    values = []
    for i in range(len(a)):
        values.append(a[i][j])
    return values


def dot(x, y):
    value = 0.0
    for i in range(len(x)):
        value += x[i] * y[i]
    return value


def gram_schmidt(a):
    n = len(a)
    q = []
    for i in range(n):
        q.append([0.0] * n)
    r = []
    for i in range(n):
        r.append([0.0] * n)

    for j in range(n):
        v = column(a, j)
        for i in range(j):
            q_column = column(q, i)
            r[i][j] = dot(q_column, v)
            for row in range(n):
                v[row] -= r[i][j] * q_column[row]
        norm = math.sqrt(dot(v, v))
        if norm == 0.0:
            raise ValueError("столбец стал нулевым, QR-разложение не строится")
        r[j][j] = norm
        for row in range(n):
            q[row][j] = v[row] / norm
    return q, r


def quadratic_roots(a, i):
    aa = a[i][i]
    b = a[i][i + 1]
    c = a[i + 1][i]
    d = a[i + 1][i + 1]
    trace = aa + d
    det = aa * d - b * c
    discriminant = trace * trace - 4.0 * det
    if discriminant >= 0.0:
        root = math.sqrt(discriminant)
        return [(trace + root) / 2.0, (trace - root) / 2.0]
    root = math.sqrt(-discriminant)
    real = trace / 2.0
    imag = root / 2.0
    return [complex(real, imag), complex(real, -imag)]


def lower_triangle_is_small(a, eps):
    n = len(a)
    for i in range(n):
        for j in range(i - 1):
            if abs(a[i][j]) >= eps:
                return False
    return True


def extract_eigenvalues(a, eps):
    if not lower_triangle_is_small(a, eps):
        return None

    n = len(a)
    eigenvalues = []
    i = 0
    while i < n:
        if i + 1 < n and abs(a[i + 1][i]) >= eps:
            if i + 2 < n and abs(a[i + 2][i + 1]) >= eps:
                return None
            eigenvalues.extend(quadratic_roots(a, i))
            i += 2
        else:
            eigenvalues.append(a[i][i])
            i += 1
    return eigenvalues


def qr_algorithm(a, eps, max_iter=5000):
    current = copy_matrix(a)
    for _ in range(max_iter):
        found = extract_eigenvalues(current, eps)
        if found is not None:
            return found
        q, r = gram_schmidt(current)
        current = multiply(r, q)
    found = extract_eigenvalues(current, eps)
    if found is None:
        raise RuntimeError("QR-алгоритм не сошёлся за отведённое число шагов")
    return found


def numpy_eigenvalues(a):
    try:
        import numpy as np
    except ImportError:
        return None
    values = np.linalg.eigvals(np.array(a, dtype=float))
    result = []
    for value in values:
        result.append(complex(value))
    return result


def qr_method(a, eps):
    q, r = gram_schmidt(a)
    return {
        "A": a,
        "Q": q,
        "R": r,
        "QR": multiply(q, r),
        "eigenvalues": qr_algorithm(a, eps),
        "numpy": numpy_eigenvalues(a),
    }
