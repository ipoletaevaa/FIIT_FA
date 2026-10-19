import math


def copy_matrix(a):
    result = []
    for row in a:
        result.append(row[:])
    return result


def identity(n):
    matrix = []
    for i in range(n):
        row = []
        for j in range(n):
            if i == j:
                row.append(1.0)
            else:
                row.append(0.0)
        matrix.append(row)
    return matrix


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


def diagonal_matrix(values):
    n = len(values)
    matrix = []
    for i in range(n):
        row = []
        for j in range(n):
            if i == j:
                row.append(values[i])
            else:
                row.append(0.0)
        matrix.append(row)
    return matrix


def max_off_diagonal(a):
    n = len(a)
    p = 0
    q = 1
    maximum = 0.0
    for i in range(n):
        for j in range(i + 1, n):
            if abs(a[i][j]) > maximum:
                maximum = abs(a[i][j])
                p = i
                q = j
    return p, q, maximum


def jacobi_method(a, eps, max_iter=1000):
    n = len(a)
    current = copy_matrix(a)
    vectors = identity(n)
    errors = []

    for _ in range(max_iter + 1):
        p, q, error = max_off_diagonal(current)
        errors.append(error)
        if error < eps:
            break

        app = current[p][p]
        aqq = current[q][q]
        apq = current[p][q]

        tau = (aqq - app) / (2.0 * apq)
        if tau >= 0.0:
            t = 1.0 / (tau + math.sqrt(1.0 + tau * tau))
        else:
            t = -1.0 / (-tau + math.sqrt(1.0 + tau * tau))
        c = 1.0 / math.sqrt(1.0 + t * t)
        s = t * c

        column_p = []
        column_q = []
        for i in range(n):
            column_p.append(current[i][p])
            column_q.append(current[i][q])

        for i in range(n):
            if i == p or i == q:
                continue
            current[i][p] = c * column_p[i] - s * column_q[i]
            current[p][i] = current[i][p]
            current[i][q] = s * column_p[i] + c * column_q[i]
            current[q][i] = current[i][q]

        current[p][p] = c * c * app - 2.0 * s * c * apq + s * s * aqq
        current[q][q] = s * s * app + 2.0 * s * c * apq + c * c * aqq
        current[p][q] = 0.0
        current[q][p] = 0.0

        old_p = []
        old_q = []
        for i in range(n):
            old_p.append(vectors[i][p])
            old_q.append(vectors[i][q])
        for i in range(n):
            vectors[i][p] = c * old_p[i] - s * old_q[i]
            vectors[i][q] = s * old_p[i] + c * old_q[i]
    else:
        raise RuntimeError("метод Якоби не сошёлся за отведённое число вращений")

    eigenvalues = []
    for i in range(n):
        eigenvalues.append(current[i][i])

    lam = diagonal_matrix(eigenvalues)
    return {
        "eps": eps,
        "eigenvalues": eigenvalues,
        "V": vectors,
        "AV": multiply(a, vectors),
        "V_lambda": multiply(vectors, lam),
        "errors": errors,
    }
