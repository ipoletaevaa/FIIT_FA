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


def lu_factor(a):
    n = len(a)
    lu = copy_matrix(a)
    p = list(range(n))
    swaps = 0

    for k in range(n):
        pivot = k
        for i in range(k + 1, n):
            if abs(lu[i][k]) > abs(lu[pivot][k]):
                pivot = i

        if pivot != k:
            lu[k], lu[pivot] = lu[pivot], lu[k]
            p[k], p[pivot] = p[pivot], p[k]
            swaps += 1

        if lu[k][k] == 0.0:
            raise ValueError("матрица вырождена: главный элемент равен нулю")

        for i in range(k + 1, n):
            lu[i][k] = lu[i][k] / lu[k][k]
            for j in range(k + 1, n):
                lu[i][j] = lu[i][j] - lu[i][k] * lu[k][j]

    l = identity(n)
    u = []
    for i in range(n):
        u_row = []
        for j in range(n):
            if j < i:
                l[i][j] = lu[i][j]
                u_row.append(0.0)
            else:
                u_row.append(lu[i][j])
        u.append(u_row)

    return l, u, p, swaps


def solve_lu(l, u, p, b):
    """Решает Ax = b, если PA = LU. Сначала Ly = Pb, затем Ux = y."""
    n = len(b)
    y = []
    for i in range(n):
        value = b[p[i]]
        for j in range(i):
            value -= l[i][j] * y[j]
        y.append(value)

    x = [0.0] * n
    for i in range(n - 1, -1, -1):
        value = y[i]
        for j in range(i + 1, n):
            value -= u[i][j] * x[j]
        x[i] = value / u[i][i]
    return x


def determinant(u, swaps):
    det = 1.0
    for i in range(len(u)):
        det *= u[i][i]
    if swaps % 2 == 1:
        det = -det
    return det


def inverse_from_lu(l, u, p):
    n = len(l)
    columns = []
    for j in range(n):
        rhs = [0.0] * n
        rhs[j] = 1.0
        columns.append(solve_lu(l, u, p, rhs))

    inverse = []
    for i in range(n):
        row = []
        for j in range(n):
            row.append(columns[j][i])
        inverse.append(row)
    return inverse


def permuted_rows(a, p):
    result = []
    for i in range(len(a)):
        result.append(a[p[i]][:])
    return result


def solve_system(a, b):
    l, u, p, swaps = lu_factor(a)
    x = solve_lu(l, u, p, b)
    inv = inverse_from_lu(l, u, p)
    return {
        "L": l,
        "U": u,
        "LU": multiply(l, u),
        "x": x,
        "inverse": inv,
        "det": determinant(u, swaps),
        "swaps": swaps,
        "check_inverse": multiply(a, inv),
        "PA": permuted_rows(a, p),
    }
